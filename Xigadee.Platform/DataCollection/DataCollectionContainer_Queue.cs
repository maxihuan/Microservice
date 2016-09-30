﻿#region Copyright
// Copyright Hitachi Consulting
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xigadee
{
    public partial class DataCollectionContainer
    {
        #region Declarations
        /// <summary>
        /// This is the concurrent queue that contains the incoming messages.
        /// </summary>
        protected ConcurrentQueue<EventHolder> mQueue;
        /// <summary>
        /// This is the mRE that is used to signal incoming messages to the thread loop.
        /// </summary>
        protected ManualResetEventSlim mReset;
        /// <summary>
        /// This is the thread loop used to log messages.
        /// </summary>
        protected Thread mThreadLog;
        /// <summary>
        /// This counter holds the current actve overload process tasks.
        /// </summary>
        protected int mOverloadProcessCount = 0;
        /// <summary>
        /// The number of overload hits.
        /// </summary>
        protected long mOverloadProcessHits = 0;
        /// <summary>
        /// This is teh number of time the overload process has been hit.
        /// </summary>
        protected int mOverloadTaskCount = 0;
        #endregion

        #region StartInternal/StopInternal
        /// <summary>
        /// This method starts the thread loop.
        /// </summary>
        protected void StartQueue()
        {
            Active = true;
            mThreadLog = new Thread(SpinWrite);
            mThreadLog.Start();
        }
        /// <summary>
        /// This override stops the logging.
        /// </summary>
        protected void StopQueue()
        {
            mReset.Set();
            Active = false;
            mThreadLog.Join();
        }
        #endregion

        #region Active
        /// <summary>
        /// This property indicates whether the collection is active.
        /// </summary>
        protected virtual bool Active
        {
            get; set;
        }
        #endregion

        #region SpinWrite(object state)
        /// <summary>
        /// This method is used to manage logging using a single thread.
        /// </summary>
        /// <param name="state">The logged state.</param>
        protected virtual void SpinWrite(object state)
        {
            while (Active)
            {
                mReset.Wait(1000);
                mReset.Reset();
                int count = ProcessQueue();
            }
        }
        #endregion
        #region ProcessQueue(int? timespaninms = null)
        /// <summary>
        /// This method will write the current items in the queue to the stream processor.
        /// </summary>
        protected virtual int ProcessQueue(int? timespaninms = null)
        {
            EventHolder logEvent;

            DateTime start = DateTime.UtcNow;

            int items = 0;
            do
            {
                while (mQueue.TryDequeue(out logEvent))
                {
                    ProcessItem(logEvent);

                    items++;

                    //Kick out every 100 loops if there is a timer limit.
                    if (timespaninms.HasValue && (items % 100 == 0))
                        break;
                }
            }
            while (mQueue.Count > 0 && (!timespaninms.HasValue || (DateTime.UtcNow - start).TotalMilliseconds < timespaninms));

            return items;
        }
        #endregion

        #region ProcessItem(EventHolder eventData)
        /// <summary>
        /// This method processes the incoming event by looping through the collectors for the particular type.
        /// </summary>
        /// <param name="eventData">The event data holder.</param>
        protected void ProcessItem(EventHolder eventData)
        {
            mCollectorSupported[eventData.DataType]?
                .ForEach((l) => ProcessItem(l, eventData));

            //Decrement the active count with the time needed to process.
            StatisticsInternal.ActiveDecrement(eventData.Timestamp);
        }
        #endregion
        #region ProcessItem(IDataCollectorComponent dataCollector, EventHolder eventData)
        /// <summary>
        /// This method wraps the individual request in a safe wrapper.
        /// </summary>
        /// <param name="dataCollector">The data collector component.</param>
        /// <param name="eventData">The event data holder.</param>
        protected virtual void ProcessItem(IDataCollectorComponent dataCollector, EventHolder eventData)
        {
            try
            {
                dataCollector.Write(eventData.DataType, eventData.Data);
            }
            catch (Exception ex)
            {
                //We don't want unexpected exceptions here and to stop the other loggers working.
                StatisticsInternal.ErrorIncrement();
                StatisticsInternal.Ex = ex;
            }
        }
        #endregion

        #region Write(EventBase eventData, DataCollectionSupport support, bool sync = false)
        /// <summary>
        /// This method writes the incoming event data to the data collectors.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        /// <param name="support">The event data type.</param>
        /// <param name="sync">Specifies whether the data should be written out immediately.</param>
        protected void Write(EventBase eventData, DataCollectionSupport support, bool sync = false)
        {
            if (!Active)
                throw new ServiceNotStartedException();

            var item = new EventHolder(support) { Data = eventData, Sync = sync, Timestamp = StatisticsInternal.ActiveIncrement() };

            if (item.Sync)
            {
                ProcessItem(item);
            }
            else
            {
                mQueue.Enqueue(item);

                mReset.Set();
            }
        } 
        #endregion

        #region Overloaded
        /// <summary>
        /// This property goes positive once the queue length exceeds the threshold amount (if set).
        /// </summary>
        public bool Overloaded { get { return mPolicy.OverloadThreshold.HasValue && mQueue.Count > mPolicy.OverloadThreshold.Value; } }
        #endregion

        #region CanProcess()
        /// <summary>
        /// This method will return true if overloaded and there are no overload tasks running.
        /// </summary>
        /// <returns>Returns true if the process can proceed.</returns>
        public virtual bool CanProcess()
        {
            return Status == ServiceStatus.Running && Overloaded && mOverloadTaskCount < mPolicy.OverloadMaxTasks;
        }
        #endregion
        #region --> Process()
        /// <summary>
        /// This method checks whether the process is overloaded and schedules a long running task to reduce the overload.
        /// </summary>
        public virtual void Process()
        {
            if (!Overloaded)
                return;

            TaskTracker tracker = new TaskTracker(TaskTrackerType.Overload, null);

            tracker.Name = GetType().Name;
            tracker.Caller = GetType().Name;
            tracker.IsLongRunning = true;
            tracker.Priority = 3;

            tracker.Execute = async (token) => await OverloadProcess();

            tracker.ExecuteComplete = (t, s, ex) => Interlocked.Decrement(ref mOverloadTaskCount);

            Interlocked.Increment(ref mOverloadTaskCount);

            TaskSubmit(tracker);
        }
        #endregion

        #region TaskSubmit
        /// <summary>
        /// This action is used to submit a task to the tracker for overflow 
        /// </summary>
        public Action<TaskTracker> TaskSubmit
        {
            get; set;
        }
        #endregion
        #region TaskAvailability
        /// <summary>
        /// This is the task availability collection
        /// </summary>
        public ITaskAvailability TaskAvailability { get; set; }
        #endregion

        #region OverloadProcess()
        /// <summary>
        /// This method allows additional schedule threads to help clear the buffer.
        /// </summary>
        /// <returns>Returns the number of items processed.</returns>
        public async Task<int> OverloadProcess()
        {
            Interlocked.Increment(ref mOverloadProcessHits);

            try
            {
                Interlocked.Increment(ref mOverloadProcessCount);
                return ProcessQueue(mPolicy.OverloadProcessTimeInMs);
            }
            finally
            {
                Interlocked.Decrement(ref mOverloadProcessCount);
            }
        }
        #endregion
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Xigadee
{
    internal class PolicyWrapper: WrapperBase, IMicroservicePolicy
    {
        #region Declarations
        object syncLock = new object();

        Microservice.Policy mPolicyMicroservice = null;
        TaskManager.Policy mPolicyTaskManager = null;
        CommandContainer.Policy mPolicyCommand = null;
        CommunicationContainer.Policy mPolicyCommunication = null;
        SchedulerContainer.Policy mPolicyScheduler = null;
        ResourceContainer.Policy mPolicyResourceTracker = null;
        DataCollectionContainer.Policy mPolicyDataCollection = null;
        ServiceHandlerContainer.Policy mPolicyServiceHandlers = null;

        /// <summary>
        /// This is the collection of policy settings for the Microservice.
        /// </summary>
        List<PolicyBase> mPolicySettings;
        #endregion

        internal PolicyWrapper(IEnumerable<PolicyBase> policySettings, Func<ServiceStatus> getStatus) : base(getStatus)
        {
            mPolicySettings = policySettings?.ToList() ?? new List<PolicyBase>();

        }

        #region PolicyResolve<P>(P existing, Action<Microservice, P> onResolve = null)
        /// <summary>
        /// This is the helper class used to pull out the policy container from the incoming collection and to set it within the correct settings.
        /// </summary>
        /// <typeparam name="P">The policy type.</typeparam>
        /// <param name="existing">The existing value. If this is not null the method will bypass the set up.</param>
        /// <param name="onResolve">An action that can be called to adjust the policy settings when it is first resolved.</param>
        /// <returns>Returns the policy.</returns>
        protected P PolicyResolve<P>(P existing, Action<P> onResolve = null)
            where P : PolicyBase, new()
        {
            if (existing == null)
            {
                lock (syncLock)
                {
                    if (existing == null)
                    {
                        existing = mPolicySettings?.Where((p) => p is P).Cast<P>().FirstOrDefault() ?? new P();
                        onResolve?.Invoke(existing);
                    }
                }
            }

            return existing;
        }
        #endregion

        #region Microservice
        /// <summary>
        /// This is the policy used to set the Microservice default settings.
        /// </summary>
        /// <returns>The microservice policy.</returns>
        public virtual Microservice.Policy Microservice
        {
            get
            {
                return PolicyResolve(mPolicyMicroservice, (p) => mPolicyMicroservice = p);
            }
        }
        #endregion
        #region TaskManager
        /// <summary>
        /// This method retrieves the policy for the task manager.
        /// </summary>
        /// <returns>The policy.</returns>
        public virtual TaskManager.Policy TaskManager
        {
            get
            {
                return PolicyResolve(mPolicyTaskManager, (p) => mPolicyTaskManager = p);
            }
        }
        #endregion
        #region ResourceTracker
        /// <summary>
        /// This is the policy for the resource tracker.
        /// </summary>
        /// <returns></returns>
        public virtual ResourceContainer.Policy ResourceMonitor
        {
            get
            {
                return PolicyResolve(mPolicyResourceTracker, (p) => mPolicyResourceTracker = p);
            }
        }
        #endregion
        #region CommandContainer
        /// <summary>
        /// This is the policy used to set the command container.
        /// </summary>
        /// <returns></returns>
        public virtual CommandContainer.Policy CommandContainer
        {
            get
            {
                return PolicyResolve(mPolicyCommand, (p) => mPolicyCommand = p);
            }
        }
        #endregion
        #region Communication
        /// <summary>
        /// This is the policy used to set the communication component settings.
        /// </summary>
        /// <returns></returns>
        public virtual CommunicationContainer.Policy Communication
        {
            get
            {
                return PolicyResolve(mPolicyCommunication, (p) => mPolicyCommunication = p);
            }
        }
        #endregion
        #region Scheduler
        /// <summary>
        /// This is the policy for the scheduler.
        /// </summary>
        public virtual SchedulerContainer.Policy Scheduler
        {
            get
            {
                return PolicyResolve(mPolicyScheduler, (p) => mPolicyScheduler = p);
            }
        }
        #endregion
        #region DataCollection
        /// <summary>
        /// This is the policy used to set the data collection container settings.
        /// </summary>
        /// <returns>The policy</returns>
        public virtual DataCollectionContainer.Policy DataCollection
        {
            get
            {
                return PolicyResolve(mPolicyDataCollection, (p) => mPolicyDataCollection = p);
            }
        }
        #endregion
        #region ServiceHandler
        /// <summary>
        /// Gets the service handler policy.
        /// </summary>
        public ServiceHandlerContainer.Policy ServiceHandlers
        {
            get
            {
                return PolicyResolve(mPolicyServiceHandlers, (p) => mPolicyServiceHandlers = p);
            }
        }
        #endregion
    }
}

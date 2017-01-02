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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xigadee
{
    public static partial class CorePipelineExtensions
    {
        public static C Inspect<C,P>(this C pipeline
            , Action<IMicroservice> msAssign = null
            , Action<IEnvironmentConfiguration> cfAssign = null
            , Action<Channel> cnAssign = null)
            where C:ChannelPipelineBase<P>
            where P:IPipeline
        {
            msAssign?.Invoke(pipeline.Pipeline.Service);
            cfAssign?.Invoke(pipeline.Pipeline.Configuration);
            cnAssign?.Invoke(pipeline.Channel);

            return pipeline;
        }


    }
}
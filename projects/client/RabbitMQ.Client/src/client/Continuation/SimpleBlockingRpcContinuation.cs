// This source code is dual-licensed under the Apache License, version
// 2.0, and the Mozilla Public License, version 1.1.
//
// The APL v2.0:
//
//---------------------------------------------------------------------------
//   Copyright (c) 2007-2016 Pivotal Software, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//---------------------------------------------------------------------------
//
// The MPL v1.1:
//
//---------------------------------------------------------------------------
//  The contents of this file are subject to the Mozilla Public License
//  Version 1.1 (the "License"); you may not use this file except in
//  compliance with the License. You may obtain a copy of the License
//  at http://www.mozilla.org/MPL/
//
//  Software distributed under the License is distributed on an "AS IS"
//  basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See
//  the License for the specific language governing rights and
//  limitations under the License.
//
//  The Original Code is RabbitMQ.
//
//  The Initial Developer of the Original Code is Pivotal Software, Inc.
//  Copyright (c) 2007-2016 Pivotal Software, Inc.  All rights reserved.
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Util;

namespace RabbitMQ.Client.Impl
{
    public class SimpleBlockingRpcContinuation : IRpcContinuation
    {
        public readonly TaskCompletionSource<Either<AssembledCommandBase<FrameBuilder>, ShutdownEventArgs>> m_cell = new TaskCompletionSource<Either<AssembledCommandBase<FrameBuilder>, ShutdownEventArgs>>();

        public virtual AssembledCommandBase<FrameBuilder> GetReply(TimeSpan timeout)
        {
            if (m_cell.Task.Wait(timeout)) {
                var result = m_cell.Task.Result;
                switch (m_cell.Task.Result.Alternative)
                {
                    case EitherAlternative.Left:
                        return m_cell.Task.Result.LeftValue;
                    case EitherAlternative.Right:
                        throw new OperationInterruptedException(m_cell.Task.Result.RightValue);
                    default:
                        ReportInvalidInvariant(m_cell.Task.Result);
                        return null;
                }
            }
            else
            {
                throw new TimeoutException();
            }
        }

        private static void ReportInvalidInvariant(Either<AssembledCommandBase<FrameBuilder>, ShutdownEventArgs> result)
        {
#if !(NETFX_CORE)
            //Trace.Fail("Illegal EitherAlternative " + result.Alternative);
#else
            MetroEventSource.Log.Error("Illegal EitherAlternative " + result.Alternative);
#endif
        }

        public virtual void HandleCommand(AssembledCommandBase<FrameBuilder> cmd)
        {
            m_cell.SetResult(Either<AssembledCommandBase<FrameBuilder>, ShutdownEventArgs>.Left(cmd));
        }

        public virtual void HandleModelShutdown(ShutdownEventArgs reason)
        {
            m_cell.SetResult(Either<AssembledCommandBase<FrameBuilder>, ShutdownEventArgs>.Right(reason));
        }
    }
}

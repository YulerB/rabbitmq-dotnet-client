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
using System.Collections.Concurrent;

namespace RabbitMQ.Client.Impl
{
    public class ObjectPool<T> where T : class
    {
        private ConcurrentBag<T> _objects;
        private Func<T> _objectGenerator;
        private Action<T> _OnPut;
        public ObjectPool(Func<T> objectGenerator, Action<T> onPut)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
            _OnPut = onPut;
        }

        public DisposableWrapper<T> GetObject()
        {
            T item;
            if (_objects.TryTake(out item))
            {
                var d = new DisposableWrapper<T>(item);
                d.Disposing += D_Disposing;
                return d;
            }
            else
            {
                var d = new DisposableWrapper<T>(_objectGenerator());
                d.Disposing += D_Disposing;
                return d;
            }
        }

        private void D_Disposing(object sender, T e)
        {
            _OnPut(e);
            _objects.Add(e);
        }
    }
}

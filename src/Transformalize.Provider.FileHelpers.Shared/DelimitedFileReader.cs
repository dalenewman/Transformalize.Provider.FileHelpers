#region license
// Transformalize
// Configurable Extract, Transform, and Load
// Copyright 2013-2017 Dale Newman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   
//       http://www.apache.org/licenses/LICENSE-2.0
//   
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System.Collections.Generic;
using System.IO;
using System.Text;
using Transformalize.Context;
using Transformalize.Contracts;

namespace Transformalize.Providers.FileHelpers {

   public class DelimitedFileReader : IRead {

      private readonly InputContext _context;
      private readonly IRowFactory _rowFactory;

      public DelimitedFileReader(InputContext context, IRowFactory rowFactory) {
         _context = context;
         _rowFactory = rowFactory;
      }

      public IEnumerable<IRow> Read() {
         var fileInfo = FileUtility.Find(_context.Connection.File);
         var encoding = Encoding.GetEncoding(_context.Connection.Encoding);
         return new DelimitedFileStreamReader(_context, new StreamReader(new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), encoding), _rowFactory).Read();
      }
   }
}


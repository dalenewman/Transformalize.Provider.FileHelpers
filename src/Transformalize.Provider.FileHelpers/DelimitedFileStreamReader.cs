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
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Context;
using Transformalize.Contracts;
using Transformalize.Extensions;
using Transformalize.Transforms;

namespace Transformalize.Providers.FileHelpers {

   public class DelimitedFileStreamReader : IRead {

      private readonly InputContext _context;
      private readonly IRowFactory _rowFactory;
      private readonly List<ITransform> _transforms = new List<ITransform>();
      private readonly StreamReader _streamReader;

      public DelimitedFileStreamReader(InputContext context, StreamReader streamReader, IRowFactory rowFactory) {
         _context = context;
         _streamReader = streamReader;
         _rowFactory = rowFactory;

         foreach (var field in context.Entity.Fields.Where(f => f.Input && f.Type != "string" && (!f.Transforms.Any() || f.Transforms.First().Method != "convert"))) {
            _transforms.Add(new ConvertTransform(new PipelineContext(context.Logger, context.Process, context.Entity, field, new Operation { Method = "convert" })));
         }
      }

      public IEnumerable<IRow> Read() {
         return _transforms.Aggregate(PreRead(), (rows, transform) => transform.Operate(rows));
      }

      private IEnumerable<IRow> PreRead() {

         _context.Debug(() => "Reading file stream.");

         var start = _context.Connection.Start;
         var end = 0;
         if (_context.Entity.IsPageRequest()) {
            start += (_context.Entity.Page * _context.Entity.Size) - _context.Entity.Size;
            end = start + _context.Entity.Size;
         }

         var current = _context.Connection.Start;

         var engine = FileHelpersEngineFactory.Create(_context);

         using (engine.BeginReadStream(_streamReader)) {
            foreach (var record in engine) {
               if (end == 0 || current.Between(start, end)) {
                  var values = engine.LastRecordValues;
                  var row = _rowFactory.Create();
                  for (var i = 0; i < _context.InputFields.Length; i++) {
                     row[_context.InputFields[i]] = values[i];
                  }
                  yield return row;
               }
               ++current;
               if (current == end) {
                  break;
               }
            }
         }

         _streamReader.Close();

         if (engine.ErrorManager.HasErrors) {
            foreach (var error in engine.ErrorManager.Errors) {
               _context.Error(error.ExceptionInfo.Message);
            }
         }

      }
   }
}


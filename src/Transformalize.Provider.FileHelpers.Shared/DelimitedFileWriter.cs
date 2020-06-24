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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Transformalize.Context;
using Transformalize.Contracts;
using Transformalize.Extensions;

namespace Transformalize.Providers.FileHelpers {

    public class DelimitedFileWriter : IWrite {
        private readonly OutputContext _context;

        public DelimitedFileWriter(OutputContext context) {
            _context = context;
        }

        public void Write(IEnumerable<IRow> rows) {

            var engine = FileHelpersEngineFactory.Create(_context);
            var fileInfo = new FileInfo(Path.Combine(_context.Connection.Folder, _context.Connection.File ?? _context.Entity.OutputTableName(_context.Process.Name)));
            var fields = _context.Entity.GetAllOutputFields().Where(f => !f.System).ToArray();

            _context.Info($"Writing {fileInfo.FullName}.");

            using (engine.BeginWriteFile(fileInfo.FullName)) {
                foreach (var row in rows) {

                    var i = 0;
                    foreach (var field in fields) {

                        switch (field.Type) {
                            case "byte[]":
                                engine[i] = Convert.ToBase64String((byte[])row[field]);
                                break;
                            case "string":
                                engine[i] = row[field];
                                break;
                            case "datetime":
                                engine[i] = field.Format == string.Empty ? ((DateTime)row[field]).ToString("o") : ((DateTime)row[field]).ToString(field.Format);
                                break;
                            default:
                                engine[i] = row[field].ToString();
                                break;
                        }
                        ++i;
                    }

                    engine.WriteNextValues();
                    _context.Entity.Inserts++;
                }
                if (engine.ErrorManager.HasErrors) {
                    var errorFile = Path.Combine(Path.GetDirectoryName(_context.Connection.File) ?? string.Empty, Path.GetFileNameWithoutExtension(_context.Connection.File) + ".errors.txt");
                    _context.Error($"File writer had {engine.ErrorManager.ErrorCount} error{engine.ErrorManager.ErrorCount.Plural()}. See {errorFile}.");
                    engine.ErrorManager.SaveErrors(errorFile);
                }
            }

        }
    }
}
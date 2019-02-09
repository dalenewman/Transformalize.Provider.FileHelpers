﻿#region license
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
using System.Linq;
using FileHelpers;
using FileHelpers.Dynamic;
using Transformalize.Context;

namespace Transformalize.Providers.FileHelpers {
    public static class FileHelpersEngineFactory {

        public static FileHelperAsyncEngine Create(OutputContext context) {

            var delimiter = string.IsNullOrEmpty(context.Connection.Delimiter) ? "," : context.Connection.Delimiter;

            var builder = new DelimitedClassBuilder(Utility.Identifier(context.Entity.OutputTableName(context.Process.Name))) {
                IgnoreEmptyLines = true,
                Delimiter = delimiter,
                IgnoreFirstLines = 0
            };

            var fields = context.Entity.GetAllOutputFields().Where(f => !f.System).ToArray();

            if (context.Connection.TextQualifier == string.Empty) {
                foreach (var field in fields) {
                    var fieldBuilder = builder.AddField(field.FieldName(), typeof(string));
                    fieldBuilder.FieldQuoted = false;
                    fieldBuilder.FieldOptional = field.Optional;
                }
            } else {
                foreach (var field in fields) {
                    var fieldBuilder = builder.AddField(field.FieldName(), typeof(string));
                    fieldBuilder.FieldQuoted = true;
                    fieldBuilder.QuoteChar = context.Connection.TextQualifier[0];
                    fieldBuilder.QuoteMultiline = MultilineMode.AllowForBoth;
                    fieldBuilder.QuoteMode = QuoteMode.OptionalForBoth;
                    fieldBuilder.FieldOptional = field.Optional;
                }
            }

            Enum.TryParse(context.Connection.ErrorMode, true, out global::FileHelpers.ErrorMode errorMode);

            FileHelperAsyncEngine engine;

            if (context.Connection.Header == Constants.DefaultSetting) {
                var headerText = string.Join(delimiter, fields.Select(f => f.Label.Replace(delimiter, " ")));
                engine = new FileHelperAsyncEngine(builder.CreateRecordClass()) {
                    ErrorMode = errorMode,
                    HeaderText = headerText,
                    FooterText = context.Connection.Footer
                };
            } else {
                engine = new FileHelperAsyncEngine(builder.CreateRecordClass()) { ErrorMode = errorMode };
                if (context.Connection.Header != string.Empty) {
                    engine.HeaderText = context.Connection.Header;
                }
                if (context.Connection.Footer != string.Empty) {
                    engine.FooterText = context.Connection.Footer;
                }
            }

            return engine;

        }

        public static FileHelperAsyncEngine Create(InputContext context) {

            var identifier = Utility.Identifier(context.Entity.OutputTableName(context.Process.Name));
            var delimiter = string.IsNullOrEmpty(context.Connection.Delimiter) ? "," : context.Connection.Delimiter;

            var builder = new DelimitedClassBuilder(identifier) {
                IgnoreEmptyLines = true,
                Delimiter = delimiter,
                IgnoreFirstLines = context.Connection.Start > 1 ? context.Connection.Start -1 : context.Connection.Start
            };

            if (context.Connection.TextQualifier == string.Empty) {
                foreach (var field in context.InputFields) {
                    var fieldBuilder = builder.AddField(field.FieldName(), typeof(string));
                    fieldBuilder.FieldQuoted = false;
                    fieldBuilder.FieldOptional = field.Optional;
                }
            } else {
                foreach (var field in context.InputFields) {
                    var fieldBuilder = builder.AddField(field.FieldName(), typeof(string));
                    fieldBuilder.FieldQuoted = true;
                    fieldBuilder.QuoteChar = context.Connection.TextQualifier[0];
                    fieldBuilder.QuoteMode = QuoteMode.OptionalForBoth;
                    fieldBuilder.FieldOptional = field.Optional;
                }
            }

            Enum.TryParse(context.Connection.ErrorMode, true, out global::FileHelpers.ErrorMode errorMode);

            var engine = new FileHelperAsyncEngine(builder.CreateRecordClass());
            engine.ErrorManager.ErrorMode = errorMode;
            engine.ErrorManager.ErrorLimit = context.Connection.ErrorLimit;

            return engine;
        }
    }
}

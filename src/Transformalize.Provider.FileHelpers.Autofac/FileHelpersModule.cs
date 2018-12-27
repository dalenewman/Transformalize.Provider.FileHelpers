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
using System.IO;
using System.Linq;
using Autofac;
using Transformalize.Configuration;
using Transformalize.Context;
using Transformalize.Contracts;
using Transformalize.Nulls;
using Transformalize.Providers.FileHelpers;

namespace Transformalize.Provider.FileHelpers.Autofac {
    public class FileHelpersModule : Module {

        protected override void Load(ContainerBuilder builder) {

            if (!builder.Properties.ContainsKey("Process")) {
                return;
            }

            var p = (Process)builder.Properties["Process"];

            // FILES
            
            // connections
            foreach (var connection in p.Connections.Where(c => c.Provider == "file")) {

                // Schema Reader
                builder.Register<ISchemaReader>(ctx => {
                    /* file and excel are different, have to load the content and check it to determine schema */
                    var fileInfo = new FileInfo(Path.IsPathRooted(connection.File) ? connection.File : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, connection.File));
                    var context = ctx.ResolveNamed<IConnectionContext>(connection.Key);
                    var cfg = new FileInspection(context, fileInfo, 100).Create();
                    var process = new Process(cfg);

                    foreach (var warning in process.Warnings()) {
                        context.Warn(warning);
                    }

                    if (process.Errors().Any()) {
                        foreach (var error in process.Errors()) {
                            context.Error(error);
                        }
                        return new NullSchemaReader();
                    }

                    return new FileSchemaReader(process, new InputContext(new PipelineContext(ctx.Resolve<IPipelineLogger>(), process, process.Entities.First())));

                }).Named<ISchemaReader>(connection.Key);
            }

            // entity input
            foreach (var entity in p.Entities.Where(e => p.Connections.First(c => c.Name == e.Connection).Provider == "file")) {

                // input version detector
                builder.RegisterType<NullInputProvider>().Named<IInputProvider>(entity.Key);

                // input read
                builder.Register<IRead>(ctx => {
                    var input = ctx.ResolveNamed<InputContext>(entity.Key);
                    var rowFactory = ctx.ResolveNamed<IRowFactory>(entity.Key, new NamedParameter("capacity", input.RowCapacity));

                    if (input.Connection.Delimiter == string.Empty && input.Entity.Fields.Count(f => f.Input) == 1) {
                        return new FileReader(input, rowFactory);
                    }
                    return new DelimitedFileReader(input, rowFactory);
                }).Named<IRead>(entity.Key);

            }

            // Entity Output
            if (p.Output().Provider == "file") {

                // PROCESS OUTPUT CONTROLLER
                builder.Register<IOutputController>(ctx => new NullOutputController()).As<IOutputController>();

                foreach (var entity in p.Entities) {

                    // ENTITY OUTPUT CONTROLLER
                    builder.Register<IOutputController>(ctx => {
                        var output = ctx.ResolveNamed<OutputContext>(entity.Key);
                        var fileInfo = new FileInfo(Path.Combine(output.Connection.Folder, output.Connection.File ?? output.Entity.OutputTableName(output.Process.Name)));
                        var folder = Path.GetDirectoryName(fileInfo.FullName);
                        var init = p.Mode == "init" || (folder != null && !Directory.Exists(folder));
                        var initializer = init ? (IInitializer)new FileInitializer(output) : new NullInitializer();
                        return new FileOutputController(output, initializer, new NullInputProvider(), new NullOutputProvider());
                    }).Named<IOutputController>(entity.Key);

                    // ENTITY WRITER
                    builder.Register<IWrite>(ctx => {
                        var output = ctx.ResolveNamed<OutputContext>(entity.Key);

                        switch (output.Connection.Provider) {
                            case "file":
                                if (output.Connection.Delimiter == string.Empty) {
                                    return new FileStreamWriter(output);
                                } else {
                                    return new DelimitedFileWriter(output);
                                }
                            default:
                                return new NullWriter(output);
                        }
                    }).Named<IWrite>(entity.Key);

                }
            }

            // FOLDERS
            foreach (var connection in p.Connections.Where(c => c.Provider == "folder")) {
                builder.Register<ISchemaReader>(ctx => new NullSchemaReader()).Named<ISchemaReader>(connection.Key);
            }
            
            // enitity input
            foreach (var entity in p.Entities.Where(e => p.Connections.First(c => c.Name == e.Connection).Provider == "folder")) {

                // input version detector
                builder.RegisterType<NullInputProvider>().Named<IInputProvider>(entity.Key);

                builder.Register<IRead>(ctx => {
                    var input = ctx.ResolveNamed<InputContext>(entity.Key);
                    var rowFactory = ctx.ResolveNamed<IRowFactory>(entity.Key, new NamedParameter("capacity", input.RowCapacity));

                    switch (input.Connection.Provider) {
                        case "folder":
                            return new FolderReader(input, rowFactory);
                        default:
                            return new NullReader(input, false);
                    }
                }).Named<IRead>(entity.Key);
            }

            if (p.Output().Provider == "folder") {
                // PROCESS OUTPUT CONTROLLER
                builder.Register<IOutputController>(ctx => new NullOutputController()).As<IOutputController>();

                foreach (var entity in p.Entities) {
                    // todo
                }
            }
        }
    }
}
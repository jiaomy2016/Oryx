﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Oryx.BuildScriptGenerator;
using Microsoft.Oryx.Common.Utilities;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;

namespace Microsoft.Oryx.BuildScriptGeneratorCli
{
    /// <summary>
    /// Configures the service provider, where all dependency injection is setup.
    /// </summary>
    internal class ServiceProviderBuilder
    {
        private IServiceCollection _serviceCollection;

        public ServiceProviderBuilder(string logFilePath = null)
        {
            LogManager.Configuration = BuildNLogConfiguration(logFilePath);
            LogManager.ReconfigExistingLoggers();

            _serviceCollection = new ServiceCollection();
            _serviceCollection
                .AddBuildScriptGeneratorServices()
                .AddCliServices()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(Extensions.Logging.LogLevel.Trace);
                    builder.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                });
        }

        public ServiceProviderBuilder ConfigureServices(Action<IServiceCollection> configure)
        {
            configure(_serviceCollection);
            return this;
        }

        public ServiceProviderBuilder ConfigureScriptGenerationOptions(Action<BuildScriptGeneratorOptions> configure)
        {
            _serviceCollection.Configure<BuildScriptGeneratorOptions>(options =>
            {
                configure(options);
            });
            return this;
        }

        public IServiceProvider Build()
        {
            return _serviceCollection.BuildServiceProvider();
        }

        private LoggingConfiguration BuildNLogConfiguration(string logPath)
        {
            var config = new LoggingConfiguration();

            var aiKey = Environment.GetEnvironmentVariable(LoggingConstants.ApplicationInsightsInstrumentationKeyEnvironmentVariableName);
            if (!string.IsNullOrWhiteSpace(aiKey))
            {
                var aiTarget = new ApplicationInsights.NLogTarget.ApplicationInsightsTarget() { Name = "ai", InstrumentationKey = aiKey };
                config.AddTarget(aiTarget);
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, aiTarget);
            }

            if (!string.IsNullOrWhiteSpace(logPath))
            {
                // Default layout: "${longdate}|${level:uppercase=true}|${logger}|${message}"
                var fileTarget = new NLog.Targets.FileTarget("file") { FileName = Path.GetFullPath(logPath), Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${exception:format=ToString}" };
                config.AddTarget(fileTarget);
                config.AddRuleForAllLevels(fileTarget);
            }

            return config;
        }
    }
}
﻿//-----------------------------------------------------------------------
// <copyright file="IExecutor.cs" company="Microsoft Corporation">
//     Copyright Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using VSMacros.Engines;

namespace VSMacros.Interfaces
{
    /// <summary>
    /// Exposes the execution engine.
    /// </summary>
    internal interface IExecutor
    {
        /// <summary>
        /// Informs subscribers of success after execution.
        /// </summary>
        event EventHandler<CompletionReachedEventArgs> Complete;

        /// <summary>
        /// Initializes the engine and then runs the macro script.
        /// </summary>
        void InitializeEngine();

        /// <summary>
        /// Will run the macro file.
        /// <param name="path">Path to macro.</param>
        /// <param name="iterations">Times to be executed.</param>
        /// </summary>
        void RunEngine(int iterations, string path);
    }
}

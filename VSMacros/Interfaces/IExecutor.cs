﻿using VSMacros.Engines;
using System;
using System.IO;

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
        /// This method will be removed after IPC is implemented.
        /// </summary>
        void InitializeEngine();

        /// <summary>
        /// Will run the macro file.
        /// <param name="macro">Name of macro.</param>
        /// <param name="iterations">Times to be executed.</param>
        /// </summary>
        void StartExecution(StreamReader reader, int iterations);

        /// <summary>
        /// Will stop the currently executing macro file.
        /// We are considering removing this.
        /// </summary>
        void StopExecution();
    }
}

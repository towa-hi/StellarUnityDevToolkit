using System;
using System.Collections.Generic;
using System.Linq;

namespace StellarSDK
{
    public class StellarClientTask
    {
        readonly Stack<string> activeSteps = new();

        public bool IsBusy => activeSteps.Count > 0;
        public string CurrentStep => activeSteps.Count > 0 ? activeSteps.Peek() : null;
        public string StepStack => activeSteps.Count > 0
            ? string.Join(" -> ", activeSteps.Reverse())
            : string.Empty;

        public event Action<string> OnStepStarted;
        public event Action<string> OnStepEnded;
        public event Action<bool> OnBusyChanged;

        public readonly struct Scope : IDisposable
        {
            readonly StellarClientTask task;
            readonly string name;

            public Scope(StellarClientTask task, string name)
            {
                this.task = task;
                this.name = name;
                task?.BeginStep(name);
            }

            public void Dispose() => task?.EndStep(name);
        }

        void BeginStep(string name)
        {
            bool wasBusy = IsBusy;
            activeSteps.Push(name);
            OnStepStarted?.Invoke(name);
            if (!wasBusy) OnBusyChanged?.Invoke(true);
        }

        void EndStep(string name)
        {
            if (activeSteps.Count > 0) activeSteps.Pop();
            OnStepEnded?.Invoke(name);
            if (!IsBusy) OnBusyChanged?.Invoke(false);
        }
    }
}

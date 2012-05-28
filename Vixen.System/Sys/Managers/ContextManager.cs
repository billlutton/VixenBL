﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Vixen.Execution;
using Vixen.Module.Timing;
using Vixen.Sys.Instrumentation;

namespace Vixen.Sys.Managers {
	public class ContextManager : IEnumerable<Context> {
		private Dictionary<Guid, Context> _instances;
		private ContextUpdateTimeValue _contextUpdateTimeValue;
		private Stopwatch _stopwatch;
		private LiveContext _systemLiveContext;

		public event EventHandler<ContextEventArgs> ContextCreated;
		public event EventHandler<ContextEventArgs> ContextReleased;

		public ContextManager() {
			_instances = new Dictionary<Guid, Context>();
			_SetupInstrumentation();
		}

		public LiveContext GetSystemLiveContext() {
			if(_systemLiveContext == null) {
				_systemLiveContext = new LiveContext("System");
				_AddContext(_systemLiveContext);
			}
			return _systemLiveContext;
		}

		public Context CreateContext(Program program) {
			ProgramContext context = new ProgramContext(program);
			_AddContext(context);

			return context;
		}

		public Context CreateContext(ISequence sequence, string contextName = null) {
			Program program = new Program(contextName ?? sequence.Name) { sequence };
			return CreateContext(program);
		}

		internal Context CreateContext(string name, IDataSource dataSource, ITiming timingSource) {
			Context context = new Context(name, dataSource, timingSource);
			_AddContext(context);
			
			return context;
		}

		public void ReleaseContext(Context context) {
			if(_instances.ContainsKey(context.Id)) {
				_ReleaseContext(context);
			}
		}

		public void ReleaseContexts() {
			foreach(Context context in _instances.Values.ToArray()) {
				ReleaseContext(context);
			}
			_instances.Clear();
		}

		public void Update() {
			lock(_instances) {
				_stopwatch.Restart();

				_instances.Values.AsParallel().ForAll(context => {
					// Get a snapshot time value for this update.
					TimeSpan contextTime = context.GetTimeSnapshot();
					IEnumerable<Guid> affectedChannels = context.UpdateChannelStates(contextTime);
					//Could possibly return affectedChannels so only affected outputs
					//are updated.  The controller would have to maintain state so those
					//outputs could be updated and the whole state sent out.
				});

				_contextUpdateTimeValue.Set(_stopwatch.ElapsedMilliseconds);
			}
		}

		public IEnumerator<Context> GetEnumerator() {
			Context[] contexts;
			lock(_instances) {
				contexts = _instances.Values.ToArray();
			}
			return contexts.Cast<Context>().GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		protected virtual void OnContextCreated(ContextEventArgs e) {
			if(ContextCreated != null) {
				ContextCreated(this, e);
			}
		}

		protected virtual void OnContextReleased(ContextEventArgs e) {
			if(ContextReleased != null) {
				ContextReleased(this, e);
			}
		}

		private void _SetupInstrumentation() {
			_contextUpdateTimeValue = new ContextUpdateTimeValue();
			VixenSystem.Instrumentation.AddValue(_contextUpdateTimeValue);
			_stopwatch = Stopwatch.StartNew();
		}

		private void _AddContext(Context context) {
			lock(_instances) {
				_instances[context.Id] = context;
			}
			OnContextCreated(new ContextEventArgs(context));
		}

		private void _ReleaseContext(Context context) {
			context.Stop();
			lock(_instances) {
				_instances.Remove(context.Id);
			}
			context.Dispose();
			OnContextReleased(new ContextEventArgs(context));
		}
	}
}

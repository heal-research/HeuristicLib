# Run cancellation and iterator cleanup

The developer accepted this fix for the `dev` release candidate after the cancellation failures were reproduced at `09827092`.

- Experiment streams reserve their trials and resolve their executors before enumeration. Each terminal iterator starts when its worker reaches the trial, with the cancellation token that also stops the experiment stream. The iterator uses the existing executor contract.
- Cancellation settles queued trial tasks and closes the progress stream even if a trial's iterator throws during cleanup. Requested cancellation takes precedence and cleanup continues for the other trials.
- An explicit algorithm run pauses at a yield boundary when its consumer stops or cancels. An executor failure ends the run and disposes its iterator once. Preserve the execution exception if cleanup also fails.

Cover stream disposal while a trial waits on its token, trial-task completion when cleanup throws, and an explicit run's terminal state and cleanup after executor failure.

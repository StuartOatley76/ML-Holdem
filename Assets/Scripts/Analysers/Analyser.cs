using System;
using System.Collections;
using Utils;

/// <summary>
/// Abstract base class for poker analysis. Contains a static StoredDictionary of type TKey,Tvalue so that a particular analysis only has to be done once
/// Analysis is multithreaded. As such we need to wait before returning the results, so there iss an event triggered when they're completed. Connect to this by
/// using SetListener
/// </summary>
/// <typeparam name="TKey">StoredDictionary Key type(A unique hashcode for the information passed into the analysis)</typeparam>
/// <typeparam name="TValue">StoredDictionary Value type(What the analysis returns)</typeparam>
/// <typeparam name="TAnalysisInputEventArgs">The type of EventArgs passed in by the event requesting analysis</typeparam>
/// <typeparam name="TAnalysisOutputEventArgs">The type of EventArgs passed out when the analysis is completed</typeparam>
public abstract class Analyser<TKey, TValue, TAnalysisInputEventArgs, TAnalysisOutputEventArgs> : PoolableObject where TAnalysisInputEventArgs : EventArgs where TAnalysisOutputEventArgs : EventArgs {

    /// <summary>
    /// The StoredDictionary containing the analyses
    /// </summary>
    protected static StoredDictionary<TKey, TValue> lookup;

    /// <summary>
    /// Event called when the Analysis is completed
    /// </summary>
    protected EventHandler<TAnalysisOutputEventArgs> Analysed;

    /// <summary>
    /// The filename for the StoredDictionary
    /// </summary>
    protected string dictionaryFileName;

    protected void Start() {

        if (lookup == null) {
            lookup = new StoredDictionary<TKey, TValue>(dictionaryFileName);
        }
    }

    /// <summary>
    /// Connects the passed in method to the event triggered when the analysis is completed and returns the method to be called when the analysis is required
    /// </summary>
    /// <param name="listener"></param>
    /// <returns></returns>
    public virtual EventHandler<TAnalysisInputEventArgs> SetListener(EventHandler<TAnalysisOutputEventArgs> listener) {

        if (Analysed != null) {
            foreach (EventHandler<TAnalysisOutputEventArgs> e in Analysed.GetInvocationList()) {
                Analysed -= e;     //No other player should have access to the analysis
            }
        }
        Analysed += listener;
        return Analyse;
    }

    /// <summary>
    /// Starts the Analysis coroutine
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="e"></param>
    protected virtual void Analyse(object obj, TAnalysisInputEventArgs e)  {
        StartCoroutine(RunAnalysisJob(e));
    }

    /// <summary>
    /// Coroutine to run analysis job. Check Dictionary for existing analysis, set up the job and start, then yield return null, complete the job,
    /// add new analysis to dictionary and put data into TAnalysisOutputEventArgs to pass to AnalysisCompleted
    /// </summary>
    /// <param name="e">Information to be analysed</param>
    /// <returns></returns>
    protected abstract IEnumerator RunAnalysisJob(TAnalysisInputEventArgs e);

    /// <summary>
    /// Triggers the Analysed event
    /// </summary>
    /// <param name="e"></param>
    protected virtual void AnalysisCompleted(TAnalysisOutputEventArgs e) {
        Analysed?.Invoke(this, e);
    }
}

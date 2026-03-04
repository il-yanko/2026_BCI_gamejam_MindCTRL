using System.Collections.Generic;
using BCIEssentials.Stimulus.Collections;
using BCIEssentials.Stimulus.Presentation;

/// <summary>
/// Wraps StimulusPresenterCollection for runtime AddComponent() use.
///
/// When Unity creates a component via AddComponent() the serialization
/// pipeline does NOT run, so [SerializeField] reference-type fields are
/// left null.  StimulusPresenterCollection._stimulusPresenters is such a
/// field — Add() would throw NullReferenceException without this fix.
/// </summary>
public class RuntimePresenterCollection : StimulusPresenterCollection
{
    void Awake()
    {
        _stimulusPresenters = new List<StimulusPresentationBehaviour>();
    }
}

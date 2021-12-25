using System;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public IState CurrentState;

    private Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();
    private List<Transition> _currentTransitions = new List<Transition>();
    private List<Transition> _anyTransitions = new List<Transition>();

    private static List<Transition> _emptyTransitions = new List<Transition>(0);

    public void Tick()
    {
        var transition = GetTransition();
        if (transition != null)
            SetState(transition.To);

        CurrentState?.Tick();
    }

    public void SetState(IState state)
    {
        if (state == CurrentState)
            return;

        CurrentState?.OnExit();
        CurrentState = state;

        _transitions.TryGetValue(CurrentState.GetType(), out _currentTransitions);
        if (_currentTransitions == null)
            _currentTransitions = _emptyTransitions;

        CurrentState?.OnEnter();

        ///Debug
        Debug.Log(CurrentState.Name);
    }

    public void AddTransition(IState from, IState to, Func<bool> condition)
    {
        if (_transitions.TryGetValue(from.GetType(), out var transitions) == false)
        {
            transitions = new List<Transition>();
            _transitions[from.GetType()] = transitions;
        }

        transitions.Add(new Transition(to, condition));
    }

    public void AddAnyTransition(IState state, Func<bool> condition)
    {
        _anyTransitions.Add(new Transition(state, condition));
    }

    private Transition GetTransition()
    {
        foreach (var transition in _anyTransitions)
            if (transition.Condition())
                return transition;

        foreach (var transition in _currentTransitions)
            if (transition.Condition())
                return transition;

        return null;
    }
}

public class Transition
{
    public IState To { get; }
    public Func<bool> Condition { get; }

    public Transition(IState to, Func<bool> condition)
    {
        To = to;
        Condition = condition;
    }
}

using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh.Automations
{
    public class Automation
    {
        public required Activation OnActivate { get; set; }

        public List<Schedule>? Schedules { get; set; }

        public Guid Id { get; set; }

        public required string Name { get; set; }

        public AutomationType AutomationType { get; set; }

        public TriggerType TriggerType { get; set; }

        public Activation? OnDeactivate { get; set; }

        public Condition? ConditionConfig { get; init; }

        public Hysteresis? HysteresisConfig { get; set; }

        public class Condition
        {
            public required string Expression { get; init; }

            public required List<Parameter> Parameters { get; init; }

            public class Parameter
            {
                public required string Name { get; set; }

                public required string Identifier { get; set; }

                public ParameterType Type { get; set; }

                public string? ValueType { get; set; } // ServiceElementType, Relevant for Property and MeasuringPoint parameter tpes
            }
        }

        public class Schedule
        {
            public ScheduleType Type { get; set; }

            public required string Value { get; set; }

            public string? TimeZone { get; set; }
        }

        public class Activation
        {
            public required List<Action> Actions { get; set; }

            public Timing? TimingConfig { get; set; }

            public class Timing
            {
                public TimingType Type { get; set; }

                public TimingUnit Unit { get; set; }

                public int Value { get; set; }
            }

            public class Action
            {
                public ActionType Type { get; set; }

                public required string Identifier { get; set; }

                public object? Value { get; set; }

                public string? ValueType { get; set; } // ServiceElementType, Relevant for SetProperty action type
            }
        }

        public class Hysteresis
        {
            public required string Raise { get; set; }

            public required string Clear { get; set; }
        }
    }

    public enum AutomationType
    {
        Rule,

        Alarm,

        Schedule,
    }

    public enum TriggerType
    {
        Condition,

        Schedule,
    }

    public enum ScheduleType
    {
        OneTime,

        Recurring,
    }

    public enum ActionType
    {
        SetProperty,

        RaiseAlarm,

        ResolveAlarm,
    }

    public enum TimingType
    {
        Sustain,
    }

    public enum TimingUnit
    {
        Seconds,

        Minutes,

        Hours,
    }

    public enum ParameterType
    {
        Property = 0,

        MeasuringPoint = 1,
    }
}
import operator as op

_OPERATORS = {
    "<": op.lt,
    "<=": op.le,
    ">": op.gt,
    ">=": op.ge,
    "==": op.eq,
    "!=": op.ne,
}


def evaluate_condition(operator_str: str, value: float, threshold: float) -> bool:
    fn = _OPERATORS.get(operator_str)
    if fn is None:
        raise ValueError(f"Unsupported operator: {operator_str}")
    return fn(value, threshold)


def desired_relay_state(rule, sensor_value: float, currently_on: bool) -> bool:
    """Whether the relay should be ON, applying hysteresis so a value sitting
    right at the threshold doesn't chatter the relay on/off every poll.

    Hysteresis only has a sensible direction for < / <= / > / >= — == and !=
    are evaluated directly against the threshold with no hysteresis band.
    """
    if rule.operator in ("<", "<="):
        off_threshold = rule.threshold + rule.hysteresis
        if currently_on:
            return evaluate_condition(rule.operator, sensor_value, off_threshold)
        return evaluate_condition(rule.operator, sensor_value, rule.threshold)

    if rule.operator in (">", ">="):
        off_threshold = rule.threshold - rule.hysteresis
        if currently_on:
            return evaluate_condition(rule.operator, sensor_value, off_threshold)
        return evaluate_condition(rule.operator, sensor_value, rule.threshold)

    return evaluate_condition(rule.operator, sensor_value, rule.threshold)

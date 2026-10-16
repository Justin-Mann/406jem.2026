from dataclasses import dataclass

import pytest

from app.rules import desired_relay_state, evaluate_condition


@dataclass
class FakeRule:
    operator: str
    threshold: float
    hysteresis: float = 0.0


def test_evaluate_condition_operators():
    assert evaluate_condition("<", 1.0, 2.0) is True
    assert evaluate_condition(">=", 2.0, 2.0) is True
    assert evaluate_condition("==", 2.0, 2.0) is True
    assert evaluate_condition("!=", 2.0, 3.0) is True


def test_evaluate_condition_unknown_operator_raises():
    with pytest.raises(ValueError):
        evaluate_condition("~=", 1, 2)


def test_less_than_rule_turns_on_below_threshold_and_off_above_hysteresis_band():
    rule = FakeRule(operator="<", threshold=1.0, hysteresis=0.2)

    # currently off: turns on only once value drops below the raw threshold
    assert desired_relay_state(rule, 1.1, currently_on=False) is False
    assert desired_relay_state(rule, 0.9, currently_on=False) is True

    # currently on: stays on until value rises past threshold + hysteresis
    assert desired_relay_state(rule, 1.1, currently_on=True) is True
    assert desired_relay_state(rule, 1.3, currently_on=True) is False


def test_greater_than_rule_turns_on_above_threshold_and_off_below_hysteresis_band():
    rule = FakeRule(operator=">", threshold=5.0, hysteresis=0.5)

    assert desired_relay_state(rule, 4.9, currently_on=False) is False
    assert desired_relay_state(rule, 5.1, currently_on=False) is True

    assert desired_relay_state(rule, 4.9, currently_on=True) is True
    assert desired_relay_state(rule, 4.4, currently_on=True) is False

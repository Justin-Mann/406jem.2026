from app.relay import Relay


def test_relay_starts_in_default_state_and_auto_mode():
    relay = Relay("valve_1", "Valve 1", pin=17, active_high=True, default_state=False)
    try:
        assert relay.is_on is False
        assert relay.mode == "auto"
    finally:
        relay.close()


def test_relay_set_changes_state_and_resets_timer():
    relay = Relay("valve_1", "Valve 1", pin=17, active_high=True, default_state=False)
    try:
        relay.set(True)
        assert relay.is_on is True
        assert relay.seconds_in_state() < 1.0

        relay.set(True)  # no-op, already on
        assert relay.is_on is True

        relay.set(False)
        assert relay.is_on is False
    finally:
        relay.close()

import pytest

from app.config import load_config


VALID_CONFIG = """
poll_interval_seconds: 3
relays:
  - id: valve_1
    name: "Valve 1"
    pin: 17
sensors:
  - id: sensor_1
    name: "Sensor 1"
    type: digital
    pin: 27
    pull_up: true
rules:
  - relay_id: valve_1
    sensor_id: sensor_1
    operator: "<"
    threshold: 1.0
"""


def test_load_valid_config(tmp_path):
    config_path = tmp_path / "config.yaml"
    config_path.write_text(VALID_CONFIG)

    config = load_config(str(config_path))

    assert config.poll_interval_seconds == 3
    assert config.relays[0].id == "valve_1"
    assert config.sensors[0].options == {"pin": 27, "pull_up": True}
    assert config.rules[0].relay_id == "valve_1"


def test_rule_referencing_unknown_relay_raises(tmp_path):
    bad_config = VALID_CONFIG.replace("relay_id: valve_1", "relay_id: does_not_exist")
    config_path = tmp_path / "config.yaml"
    config_path.write_text(bad_config)

    with pytest.raises(ValueError, match="unknown relay_id"):
        load_config(str(config_path))


def test_duplicate_relay_ids_raise(tmp_path):
    dup_config = VALID_CONFIG.replace(
        "sensors:",
        "  - id: valve_1\n    name: \"Duplicate\"\n    pin: 5\nsensors:",
    )
    config_path = tmp_path / "config.yaml"
    config_path.write_text(dup_config)

    with pytest.raises(ValueError, match="Duplicate relay ids"):
        load_config(str(config_path))

import pytest

from app.sensors.digital import DigitalGpioSensor
from app.sensors.pulse import PulseCounterSensor


def test_digital_sensor_reads_pin_state():
    sensor = DigitalGpioSensor("s1", "Test Sensor", pin=4, pull_up=False)
    try:
        sensor._device.pin.drive_high()
        assert sensor.read() == 1.0

        sensor._device.pin.drive_low()
        assert sensor.read() == 0.0
    finally:
        sensor.close()


def test_pulse_counter_computes_rate_from_pulse_count():
    sensor = PulseCounterSensor("f1", "Flow Meter", pin=6, pulses_per_liter=60, pull_up=False)
    try:
        # Simulate 30 pulses = 0.5L; force the elapsed window to exactly 30s
        # so the resulting rate is a clean, easily-asserted number.
        for _ in range(30):
            sensor._on_pulse()
        sensor._last_read_time -= 30

        rate = sensor.read()

        assert rate == pytest.approx(1.0, rel=0.05)
    finally:
        sensor.close()

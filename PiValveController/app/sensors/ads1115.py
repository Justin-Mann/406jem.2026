from .base import Sensor

# One shared ADS1115 chip instance per I2C address, since several sensors may
# read different channels off the same physical breakout board.
_ADS_INSTANCES = {}


class ADS1115Sensor(Sensor):
    """An analog sensor (soil moisture, analog water level, etc.) read
    through an ADS1115 I2C ADC, since the Pi has no native ADC."""

    def __init__(self, id, name, channel, i2c_address=0x48, gain=1):
        super().__init__(id, name)
        try:
            import board
            import busio
            import adafruit_ads1x15.ads1115 as ADS
            from adafruit_ads1x15.analog_in import AnalogIn
        except ImportError as e:
            raise RuntimeError(
                "ADS1115 sensor support requires 'adafruit-blinka' and "
                "'adafruit-circuitpython-ads1x15' to be installed on the Pi."
            ) from e

        ads = _ADS_INSTANCES.get(i2c_address)
        if ads is None:
            i2c = busio.I2C(board.SCL, board.SDA)
            ads = ADS.ADS1115(i2c, address=i2c_address)
            ads.gain = gain
            _ADS_INSTANCES[i2c_address] = ads

        channel_pins = [ADS.P0, ADS.P1, ADS.P2, ADS.P3]
        self._analog_in = AnalogIn(ads, channel_pins[channel])

    def read(self) -> float:
        return self._analog_in.voltage

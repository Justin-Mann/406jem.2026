from abc import ABC, abstractmethod


class Sensor(ABC):
    def __init__(self, id, name):
        self.id = id
        self.name = name

    @abstractmethod
    def read(self) -> float:
        ...

    def close(self):
        pass

from mne_lsl.lsl import StreamInfo, StreamOutlet
from .messenger import Messenger
from ..classification.generic_classifier import Prediction
import numpy as np

__all__ = ["LslMessenger"]


class LslMessenger(Messenger):
    """A Messenger object for sending event messages to an LSL outlet."""

    def __init__(self):
        """Create an LslMessenger object.

        If the LSL outlet cannot be created, an exception is raised."""
        try:
            info = StreamInfo(
                name="PythonResponse",
                stype="BCI_Essentials_Predictions",
                n_channels=1,
                sfreq=0,  # 0 means irregular rate
                dtype="string",
                source_id="pyp30042",
            )
            self.__outlet = StreamOutlet(info)
            self.__outlet.push_sample(["This is the python response stream"])
        except Exception:
            raise Exception("LslMessenger: could not create outlet")

    def ping(self):
        self.__outlet.push_sample(["ping"])

    def marker_received(self, marker):
        # ignore
        pass

    def prediction(self, prediction: Prediction):
        prediction_message = self.format_prediction_message(prediction)
        self.__outlet.push_sample([prediction_message])

    def format_prediction_message(self, prediction: Prediction) -> str:
        labels = prediction.labels
        probabilities = prediction.probabilities

        # One label, list of scalars
        if np.isscalar(probabilities[0]):
            return self.format_constituent_prediction_string(labels[0], probabilities)

        # One or more label, nested list of scalars
        constituent_prediction_strings = []
        for label_index in range(len(labels)):
            label = int(labels[label_index])
            label_probabilities = probabilities[label_index]

            constituent_prediction_strings.append(
                self.format_constituent_prediction_string(label, label_probabilities)
            )

        return ",".join(constituent_prediction_strings)

    def format_constituent_prediction_string(
        self,
        label: int,
        probabilities: list | np.ndarray,
        probability_precision: int = 4,
    ) -> str:
        probability_format = "%.{}f".format(probability_precision)
        probabilities_string = " ".join([probability_format % p for p in probabilities])

        return "%s:[%s]" % (str(label), probabilities_string)

import pandas as pd
import numpy as np
import xgboost as xgb
from modules.factory.Operation import Operation
from models.abstract.model import Model
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_squared_error

class XGBoost(Model):
    def __init__(self, csv_file=None):
        self.observed_data = self.read_from_observed_csv(csv_file)
        if self.observed_data is None or self.observed_data.empty:
            raise ImportError("No observed data found, using pre-existing data.")
        
        # Train XGBoost model
        self.model = None
        self.train_xgboost_model(self.observed_data)

    def inference(self, operation: Operation) -> int:
        
        inferenced_variables = self.infer_duration(operation)

        new_duration = round(operation.duration * inferenced_variables['relative_processing_time_deviation'],0)

        return new_duration
    
    def read_from_observed_csv(self, file):
        data = pd.read_csv(file)
        data.drop(columns=data.columns[0], axis=1, inplace=True)
        return data
    
    def train_xgboost_model(self, data):
        """
        Trains an XGBoost model on the provided data.
        
        :param data: The input data for training the model (as pandas DataFrame).
        """
        # Prepare features and target
        features = data.drop(columns=['relative_processing_time_deviation'])  # Assuming 'relative_processing_time_deviation' is the target variable
        target = data['relative_processing_time_deviation']
        
        # Train-test split
        X_train, X_test, y_train, y_test = train_test_split(features, target, test_size=0.2, random_state=42)

        # XGBoost DMatrix
        dtrain = xgb.DMatrix(X_train, label=y_train)
        dtest = xgb.DMatrix(X_test, label=y_test)

        # Specify model parameters
        params = {
            'objective': 'reg:squarederror',  # Regression problem
            'max_depth': 6,
            'eta': 0.1,
            'subsample': 0.8,
            'colsample_bytree': 0.8,
            'eval_metric': 'rmse'
        }
        
        # Train the model
        self.model = xgb.train(params, dtrain, num_boost_round=100, evals=[(dtest, 'test')], early_stopping_rounds=10)
        
        # Evaluate model
        predictions = self.model.predict(dtest)
        mse = mean_squared_error(y_test, predictions)
        print(f"Model MSE: {mse}")
        
    def predict(self, features):
        """
        Makes predictions using the trained XGBoost model.

        :param features: The input features for which to make predictions (as pandas DataFrame).
        :return: The predicted target values.
        """
        if self.model is None:
            raise ValueError("Model is not trained yet.")
        
        dfeatures = xgb.DMatrix(features)
        predictions = self.model.predict(dfeatures)
        return predictions

    def save_model(self, model_filename='xgboost_model.json'):
        """
        Saves the trained XGBoost model to a file.
        
        :param model_filename: Path to the file where the model will be saved.
        """
        if self.model is None:
            raise ValueError("Model is not trained yet.")
        
        self.model.save_model(model_filename)
        print(f"Model saved to {model_filename}")
    
    def load_model(self, model_filename='xgboost_model.json'):
        """
        Loads an XGBoost model from a file.
        
        :param model_filename: Path to the file where the model is stored.
        """
        self.model = xgb.Booster()
        self.model.load_model(model_filename)
        print(f"Model loaded from {model_filename}")

    def inference_duration(self, operation: Operation):
        """
        Infers the processing duration using the trained XGBoost model.
        
        :param operation: Operation object containing the relevant features for inference.
        :param use_truth_model: Whether to use the truth model or the learned model.
        :return: Predicted processing duration.
        """
        if not self.model:
            raise ValueError("Model is not trained yet.")
        
        if operation.machine is not None:
            previous_machine_pause =  operation.tool != operation.machine.current_tool
        else:
            previous_machine_pause = True
            
        evidence = {
            'previous_machine_pause': previous_machine_pause
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }

        previous_machine_pause =  operation.tool != tool
        features = {
            'previous_machine_pause': previous_machine_pause
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }

        # Convert to DataFrame
        feature_df = pd.DataFrame([features])

        # Predict duration
        predicted_duration = self.predict(feature_df)
        return predicted_duration[0]


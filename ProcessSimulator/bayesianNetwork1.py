from pgmpy.models import BayesianNetwork
from pgmpy.factors.discrete import TabularCPD, DiscreteFactor
from pgmpy.inference import VariableElimination

import numpy as np
from scipy.stats import norm

model = BayesianNetwork([('Temperature', 'DurationFactor'), ('IsWorkingDay', 'DurationFactor'), ('DaysSinceLastInterrupt', 'DurationFactor'), ('Shift', 'DurationFactor')])

# temperature discretized to <19, 19-21, >21
cpd_temperature = TabularCPD(variable='Temperature', variable_card=3, values=[[0.333], [0.333], [0.333]], state_names={'Temperature': ['<19', '19-21', '>21']})

cpd_isWorkingDay = TabularCPD(variable='IsWorkingDay', variable_card=2, values=[[0.5], [0.5]], state_names={'IsWorkingDay': ['False', 'True']})

# days since last interrupt discretized to  <3.5, 3.5-4.5, 4.5-5, >5
cpd_daysSinceLastInterrupt = TabularCPD(variable='DaysSinceLastInterrupt', variable_card=4, values=[[0.25], [0.25], [0.25], [0.25]], state_names={'DaysSinceLastInterrupt': ['<3.5', '3.5-4.5', '4.5-5', '>5']})

cpd_shift = TabularCPD(variable='Shift', variable_card=2, values=[[0.5], [0.5]], state_names={'Shift': ['Day', 'Night']})

#create an array with 6 rows and 48 columns
duration_factor_cpd_values = np.zeros((6, 48))

def calculate_duration_factor(temperature_str, isWorkingDay_str, daysSinceLastInterrupt_str, shift_str):
    match temperature_str:
        case "<19": temperature_factor = 0.9
        case "19-21": temperature_factor = 1
        case ">21": temperature_factor = 1.1
        
    match daysSinceLastInterrupt_str:
        case "<3.5": daysSinceLastInterrupt_factor = 0.923
        case "3.5-4.5": daysSinceLastInterrupt_factor = 0.98
        case "4.5-5": daysSinceLastInterrupt_factor = 1.06
        case ">5": daysSinceLastInterrupt_factor = 1.1
    
    if isWorkingDay_str == "False" and shift_str == "Night":
        night_shift_weekend_factor = 1.1
    else:
        night_shift_weekend_factor = 1
        
    duration_factor = temperature_factor * daysSinceLastInterrupt_factor * night_shift_weekend_factor
    possible_duration_factors = [0.8, 0.9, 1, 1.1, 1.2, 1.3]
    
    
    # calculate the closest possible duration factor to the calculated duration factor and set the probability of that duration factor to 1, all other probabilities are 0
    # result = np.zeros(6)
    # result[np.argmin(np.abs(np.array(possible_duration_factors) - duration_factor))] = 1
    
    # calculate the probability of each possible duration factor using the normal distribution
    result = []
    prev_value = 0
    for possible_duration_factor in possible_duration_factors:
        value = norm.cdf(possible_duration_factor + 0.05, loc=duration_factor, scale=0.04)
        result.append(value - prev_value)
        prev_value = value
        
    result[5] += 1 - sum(result)
        
    return result

#fill the array with the values by iterating over the possible values of the variables and calculating the duration factor using the function below
cur_col = 0
for temperature in ["<19", "19-21", ">21"]:
    for isWorkingDay in ["False", "True"]:
        for daysSinceLastInterrupt in ["<3.5", "3.5-4.5", "4.5-5", ">5"]:
            for shift in ["Day", "Night"]:
                duration_factor_cpd_values[:,cur_col] = calculate_duration_factor(temperature, isWorkingDay, daysSinceLastInterrupt, shift)
                cur_col += 1

print(duration_factor_cpd_values)
        

cpd_durationFactor = TabularCPD(
    variable='DurationFactor', 
    variable_card=6,
    values=duration_factor_cpd_values,
    evidence=['Temperature', 'IsWorkingDay', 'DaysSinceLastInterrupt', 'Shift'],
    evidence_card=[3, 2, 4, 2],
    state_names={
        'DurationFactor': ['0.8', '0.9', '1', '1.1', '1.2', '1.3'],
        'Temperature': ['<19', '19-21', '>21'],
        'IsWorkingDay': ['False', 'True'],
        'DaysSinceLastInterrupt': ['<3.5', '3.5-4.5', '4.5-5', '>5'],
        'Shift': ['Day', 'Night']
    }
)

model.add_cpds(cpd_temperature, cpd_isWorkingDay, cpd_daysSinceLastInterrupt, cpd_shift, cpd_durationFactor)
print(cpd_durationFactor)
cpd_durationFactor.to_csv("duration_factor_cpd.csv")
print(model.check_model())

inference = VariableElimination(model)

def infer(temperature, isWorkingDay, daysSinceLastInterrupt, shift):
    # discretize temperature
    if temperature < 19:
        temperature = "<19"
    elif temperature > 21:
        temperature = ">21"
    else:
        temperature = "19-21"
        
    # discretize days since last interrupt
    if daysSinceLastInterrupt < 3.5:
        daysSinceLastInterrupt = "<3.5"
    elif daysSinceLastInterrupt > 5:
        daysSinceLastInterrupt = ">5"
    elif daysSinceLastInterrupt > 4.5:
        daysSinceLastInterrupt = "4.5-5"
    else:
        daysSinceLastInterrupt = "3.5-4.5"
        
    isWorkingDay = "True" if isWorkingDay else "False"
    
    result:DiscreteFactor = inference.query(['DurationFactor'], evidence={'Temperature': temperature, 'IsWorkingDay': isWorkingDay, 'DaysSinceLastInterrupt': daysSinceLastInterrupt, 'Shift': shift})
    return float(result.sample(1)["DurationFactor"][0])
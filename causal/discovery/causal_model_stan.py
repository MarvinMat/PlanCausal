from cmdstanpy import CmdStanModel
#from cmdstanpy import install_cmdstan

#install_cmdstan()

# Assuming the Stan model is saved as "operation_model.stan"
model = CmdStanModel(stan_file='causal/operation_model.stan')

# Data dictionary
data = {
    'N': 4,
    'operation_pres_count': [0, 1, 2, 3],
    'operation_req_machine': [0, 1, 2, 3],
    'operation_duration': [5.0, 6.0, 7.0, 8.0]
}

# Fit the model
fit = model.sample(data=data)
print(fit.summary())

from pomegranate import DiscreteDistribution, BayesianNetwork, State, ConditionalProbabilityTable

# Discrete nodes
d1 = DiscreteDistribution({'0': 0.25, '1': 0.25, '2': 0.25, '3': 0.25})
d2 = DiscreteDistribution({'a1': 0.25, 'a2': 0.25, 'a3': 0.25, 'a4': 0.25})

# Conditional node
# Example: assuming you have defined the table correctly elsewhere
d3 = ConditionalProbabilityTable([...], [d1, d2])

# Create states
s1 = State(d1, name="operation_pres_count")
s2 = State(d2, name="operation_req_machine")
s3 = State(d3, name="operation_duration")

# Create and add to the Bayesian Network
model = BayesianNetwork("Operation Duration Model")
model.add_states(s1, s2, s3)
model.add_edge(s1, s3)
model.add_edge(s2, s3)
model.bake()

# Querying the model
result = model.predict_proba({'operation_pres_count': '0', 'operation_req_machine': 'a2'})
print(result)

data {
    int<lower=0> N; // number of data points
    array[N] int<lower=0, upper=3> operation_pres_count;
    array[N] int<lower=0, upper=3> operation_req_machine;
    array[N] real operation_duration;
}
parameters {
    real pres_count_effect;
    real machine_effect;
    real<lower=0> sigma; // standard deviation
}
model {
    operation_duration ~ normal(pres_count_effect * operation_pres_count + machine_effect * operation_req_machine, sigma);
}

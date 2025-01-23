def monitorEvent(data, t, prio, eid, event):
    data.append((t, eid, type(event)))


def monitorResource(data, resource):
    """This is our monitoring callback."""
    item = (
        resource.id,
        resource.current_operation,
        resource._env.now,  # The current simulation time
        resource.count,  # The number of users
        len(resource.queue),  # The number of queued processes
    )
    data.append(item)

def monitorSubject(subject, resource):
    """This is our monitoring callback."""
    subject.on_next([
        resource._env.now,  # The current simulation time
        resource.count,  # The number of users
        len(resource.queue)]  # The number of queued processes
    )
    
# boot up
# schedule your shit, creating jobs_data with shape 
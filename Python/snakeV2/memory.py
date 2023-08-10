import time

class Memory:
  def __init__(self, verbose = 0):
    self.verbose = verbose
    if self.verbose == 1:
      print('Memory init')
    self.clear()

  # Resets/restarts the memory buffer
  def clear(self): 
    if self.verbose == 1:
      print('Memory clear')
    self.observations = []
    self.actions = []
    self.rewards = []

  # Add observations, actions, rewards to memory
  def add_to_memory(self, new_observation, new_action, new_reward): 
    if self.verbose == 1:
      print('Memory add_to_memory')
    self.observations.append(new_observation)
    self.actions.append(new_action)
    self.rewards.append(new_reward)

class TimeLogger:
  def __init__(self, verbose = 0):
    self.verbose = verbose
    if self.verbose == 1:
      print('Memory init')
    self.result = {}
    self.time = {}

  def clear(self): 
    self.result.clear()
    self.time.clear()

  def start(self, name):
    self.time[name] = time.time()
    return name

  def stop(self, name):
    if name not in self.time:
      return 0.0
    result = time.time() - self.time[name]
    del self.time[name]
    if name not in self.result:
      self.result[name] = 0.0
    self.result[name] += result
    return result
  
  def print(self):
    if 'all' in self.result:
      total = self.result['all']
    for k, v in self.result.items():
      if v > 0.0:
        if total > 0.0:
          print('{}: {:.2f} ({:.1f}%)'.format(k, v, 100.0*v/total))
        else:
          print('{}: {:.2f}'.format(k, v))

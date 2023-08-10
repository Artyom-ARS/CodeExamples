from game import Env
from display import Render
from trainer import SnakeExecutor
import numpy as np

class Integrator:
  def __init__(self, verbose=0):
    self.verbose = verbose
    if self.verbose == 1:
      print('Integrator init')

  def _env_init(self):
    self.env.seed(1)
    observation = self.env.reset()
    observation = np.reshape(observation, (self.env.field_width, self.env.field_height, 1))
    return observation

  def _env_step(self, action):
    observation, reward, done, info = self.env.step(action)
    observation = np.reshape(observation, (self.env.field_width, self.env.field_height, 1))

    return observation, reward, done, info

  def _make_step(self, prev_observation):
    if prev_observation == None:
      prev_observation = self.env.reset()
    observation = np.reshape(prev_observation, (self.env.field_width, self.env.field_height, 1))
    observation = np.expand_dims(observation, axis=0)
    action = np.argmax(self.test_model.predict(observation))
    observation, _, _, _ = self.env.step(action)
    return observation

  def run_training(self, complexity, model_version, field_width=30, field_height=30, iterations=200000):
    self.env = Env(complexity, field_width=field_width, field_height=field_height, verbose=self.verbose)
    input_size = self.env.get_observation_space()
    output_size = self.env.get_action_space_count()
    suffix = '{}x{}_{}'.format(self.env.field_width, self.env.field_height, self.env.complexity)
    executor = SnakeExecutor(input_size, output_size, suffix, version=model_version, verbose=self.verbose)
    executor.run_training(iterations, init_function=self._env_init, step_function=self._env_step)

  def run_test(self, complexity, model_version, field_width=30, field_height=30, speed=50, iterations=500):
    self.env = Env(complexity, field_width=field_width, field_height=field_height, verbose=self.verbose)
    input_size = self.env.get_observation_space()
    output_size = self.env.get_action_space_count()
    suffix = '{}x{}_{}'.format(self.env.field_width, self.env.field_height, self.env.complexity)
    executor = SnakeExecutor(input_size, output_size, suffix, version=model_version, verbose=self.verbose)
    self.test_model = executor.clone_model()
    _ = Render(self.env.field_width, self.env.field_height, self._make_step, speed=speed, steps=iterations)

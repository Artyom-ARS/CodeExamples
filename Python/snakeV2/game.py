import random
import math
import numpy as np

'''
complexity:
apple apperas on the middle positon with +/- 10 positions
  and snake appears in range of 5 cells
  and snake appears not on the same line with apple
  and snake has no body
  and snake head ignoring all collisions -> = 0
0 + get small positive reward if goes continuesly to apple during last 5 steps -> 1
1 + snake appears anywhere -> 2
2 + apple appears anywhere -> 3
3 + collisions with borders ->  4
4 + snake has fixed body size, starts in range 1-15 -> 5
5 + snake body grows when snake eats apple, but size always starts from 1 -> 6
6 + snake body collisions == all possible -> 100
'''   

'''
abs_direction:
LEFT -> direction = 0
RIGHT -> direction = 1
DOWN -> direction = 2
UP -> direction = 3
'''     

class Env:
  def __init__(self, complexity=100, min_iterations=500, max_iterations=1000,
    field_width = 50, field_height = 50, verbose = 0):
    self.verbose = verbose
    if self.verbose == 1:
      print('Env init')
    self.field_width = field_width
    self.field_height = field_height
    self.done_reward = 10.0
    self.snake_position = []
    self.min_iterations = min_iterations
    self.max_iterations = max_iterations
    self.complexity = complexity

  # Resets env
  def reset(self): 
    if self.verbose == 1:
      print('Env reset')
    self.__make_starting_positions()
    self.info = []
    self.negative_reward = 0.0
    self.positive_reward = 0.0
    self.__fill_observation()
    self.iteration = 0
    self.go_to_steps = 0
    self.done = False

    return self.observation

  # Env step
  def step(self, action): 
    if self.verbose == 1:
      print('Env step')

    if self.done:
      return None, None, None, None

    self.reward = 0.0
    self.done = False
    
    before_distance = self.__calculate_distance()
    self.__move_snake(action)
    self.__fill_observation()
    self.__update_distance_reward(self.__calculate_distance() - before_distance)
    self.iteration += 1
    
    return self.observation, self.reward, self.done, self.info

  def seed(self, seed):
    random.seed(seed)

  def get_observation_space(self):
    return self.field_width, self.field_height, 1

  def get_action_space_count(self):
    return 4

  def get_action_space_sample(self):
    return random.randrange(0, self.get_action_space_count())

  def __calculate_distance(self):
    dist = np.linalg.norm(np.array(self.apple_position) - np.array(self.snake_start))
    return dist

  def __update_distance_reward(self, distance_diff):
    if self.verbose == 1:
      print('distance_diff:', distance_diff)
    if self.complexity > 0:
      if self.reward == 0.0:
        if self.go_to_steps >= 5:
          self.reward = 0.5
          self.positive_reward += self.reward
          self.go_to_steps = 0
          if self.iteration > self.max_iterations or (self.iteration > self.min_iterations and self.reward != 0.0) :
            self.done = True
          return
        if distance_diff > 0.0:
          self.go_to_steps = 0
        else:
          self.go_to_steps += 1
      else:
        self.go_to_steps = 0

  def __make_starting_positions(self):
    self.snake_position.clear()
    max_score = 25
    if max_score > self.field_width:
      max_score = self.field_width - 1
    self.score = random.randrange(5, max_score)
    if self.complexity < 5:
      self.score = 0

    snake_start_min_x = self.score+1
    snake_start_min_y = 1
    snake_start_max_x = self.field_width-2
    snake_start_max_y = self.field_height-2

    if snake_start_max_x <= snake_start_min_x:
      snake_start_max_x = snake_start_min_x

    if self.score > snake_start_max_x - snake_start_min_x:
      self.score = snake_start_max_x - snake_start_min_x

    if self.complexity > 2:
      snake_start_x = random.randrange(snake_start_min_x, snake_start_max_x+1)
      snake_start_y = random.randrange(snake_start_min_y, snake_start_max_y+1)
      self.snake_start = [snake_start_x, snake_start_y]
      for i in range(self.score):
          self.snake_position.append([snake_start_x-(i), snake_start_y])

    self.__make_new_apple_position()

    if self.complexity < 2:
      snake_start_min_x = self.apple_position[0] - 5
      snake_start_max_x = self.apple_position[0] + 5
      snake_start_min_y = self.apple_position[1] - 5
      snake_start_max_y = self.apple_position[1] + 5

    if self.complexity < 3:
      snake_start_x = random.randrange(snake_start_min_x, snake_start_max_x+1)
      snake_start_y = random.randrange(snake_start_min_y, snake_start_max_y+1)
      while snake_start_x in range(self.apple_position[0]-1,self.apple_position[0]+2):
        snake_start_x += 1
      while snake_start_y in range(self.apple_position[1]-1,self.apple_position[1]+2):
        snake_start_y += 1
      self.snake_start = [snake_start_x, snake_start_y]
      self.snake_position.append([snake_start_x, snake_start_y])

    if self.verbose == 1:
      print('Starting posiotion snake head x:{}, y:{}, snake second x: {}, y: {}'.format(self.snake_position[0][0], self.snake_position[0][1], \
        self.snake_position[1][0], self.snake_position[1][1]))

  def __make_new_apple_position(self):
    apple_rand = 5

    if self.complexity < 3:
      snake_add_x = random.randrange(-apple_rand, apple_rand+1)
      snake_add_y = random.randrange(-apple_rand, apple_rand+1)
      self.apple_position = [int(self.field_width/2) + snake_add_x, int(self.field_height/2) + snake_add_y]
      return

    self.apple_position = [self.snake_start[0], self.snake_start[1]]
    while self.apple_position in self.snake_position:
        self.apple_position = [random.randrange(1, self.field_width-2), random.randrange(1, self.field_height-2)]

  def __move_snake(self, abs_direction):
    new_x, new_y = self.__move_snake_start(abs_direction, self.snake_start[0], self.snake_start[1])
    snake_start = [new_x, new_y]
    if self.verbose == 1:
      print('Snake head old x:{}, y:{}'.format(self.snake_start[0], self.snake_start[1]))

    fail = False
    fail = fail or (self.complexity > 3 and self.__collision_with_boundaries(new_x, new_y))
    fail = fail or (self.complexity > 6 and self.__collision_with_self(new_x, new_y))

    if fail:
      self.reward = -1.0
      self.negative_reward += -self.reward

    if fail:
      self.__make_starting_positions()
    else:
      snake_start[0] = self.__fix_out_of_field_coordinate(new_x, self.field_width)
      snake_start[1] = self.__fix_out_of_field_coordinate(new_y, self.field_height)

      if snake_start == self.apple_position:
        self.reward = 1.0
        self.score += 1
        self.positive_reward += self.reward
        self.snake_position.insert(0, list(snake_start))
      else:
        self.snake_position.insert(0, list(snake_start))
        self.snake_position.pop()

      self.snake_start = snake_start

      if snake_start == self.apple_position:
        if self.complexity < 3:
          self.__make_starting_positions()
        else:
          self.__make_new_apple_position()

    if self.iteration > self.max_iterations or (self.iteration > self.min_iterations and self.reward != 0.0) :
      self.done = True

    if self.complexity < 4 and self.done and self.reward == 0.0:
      self.reward = -1.0
      self.negative_reward += -self.reward

    if self.verbose == 1:
      print('Snake head new x:{}, y:{}'.format(self.snake_start[0], self.snake_start[1]))
      print('Snake head from position new x:{}, y:{}'.format(self.snake_position[0][0], self.snake_position[0][1]))

  def __fix_out_of_field_coordinate(self, coord, size):
    if coord < 1:
      return size - 2
    if coord >= size-1:
      return 1
    return coord

  def __move_snake_start(self, abs_direction, x, y):
    if abs_direction == 1:
        x += 1
    elif abs_direction == 0:
        x -= 1
    elif abs_direction == 2:
        y += 1
    else:
        y -= 1
    return x, y
    
  def __collision_with_self(self, x, y):
    if [x, y] in self.snake_position[1:-1]:
      return True
    else:
      return False

  def __collision_with_boundaries(self, x, y):
    if x < 1 or y < 1 or x >= self.field_width-1 or y >= self.field_height-1:
      return True
    return False

  def __fill_observation(self):
    self.observation = [[0.0] * self.field_width for i in range(self.field_height)]
    for position in self.snake_position[1:]:
      self.observation[position[0]][position[1]] = 0.2
    for i in range(self.field_width):
      self.observation[i][0] = 0.1
      self.observation[i][self.field_height-1] = 0.1
    for i in range(self.field_height):
      self.observation[0][i] = 0.1
      self.observation[self.field_width-1][i] = 0.1
    self.observation[self.snake_start[0]][self.snake_start[1]] = 0.9
    self.observation[self.apple_position[0]][self.apple_position[1]] = 1.0
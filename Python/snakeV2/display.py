import numpy as np
import arcade

class Render:
  def __init__(self, field_width, field_height, game_step_function = None, steps = 100, speed = 5, verbose = 0):
    self.verbose = verbose
    if self.verbose == 1:
      print('Render init')
    self.color_green = (0, 255, 0)
    self.color_red = (255, 0, 0)
    self.color_black = (0, 0, 0)
    self.color_white = arcade.color.WHITE
    self.color_yellow = (255,220,50)
    self.color_orange = (255,165,0)
    self.cell_size = 10
    self.field_height = field_height
    self.field_width = field_width
    self.display_width = self.field_width * self.cell_size
    self.display_height = self.field_height * self.cell_size
    self.speed = speed
    self.game_step_function = game_step_function
    self.steps = steps
    self.current_step = 0
    self.run = True
    self.observation = None
    arcade.open_window(self.display_width, self.display_height, "SCORE: 0")
    arcade.set_background_color(arcade.color.WHITE)
    arcade.schedule(self.__draw_step, 1 / self.speed )
    arcade.run()

  def __draw_step(self, delta_time):

    if self.run == False:
      return

    if self.current_step > self.steps:
      self.run = False
      arcade.window_commands.close_window()
      return

    arcade.window_commands._window.set_caption("STEP: {}".format(self.current_step))

    self.current_step += 1

    if self.game_step_function == None:
      return
    
    self.observation = self.game_step_function(self.observation)

    arcade.start_render()

    arcade.draw_xywh_rectangle_filled(0, 0, self.display_width, self.display_height, self.color_white)
    
    for i in range(self.field_height):
      for j in range(self.field_width):
        val = self.observation[j][i]
        if val > 0.0:
          color = self.color_white
          if val == 0.9:
            color = self.color_orange
          if val == 1.0:
            color = self.color_green
          if val == 0.2:
            color = self.color_yellow
          if val == 0.1:
            color = self.color_black
          self.__draw(j, self.field_height - i - 1, color)

  def __draw(self, x, y, color):
    arcade.draw_xywh_rectangle_filled(x * self.cell_size, y * self.cell_size, self.cell_size, self.cell_size, color)
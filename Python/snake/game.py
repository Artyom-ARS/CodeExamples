import pygame
import random
import math
import numpy as np

green = (0, 255, 0)
red = (255, 0, 0)
black = (0, 0, 0)
white = (255, 255, 255)
yellow = (255,220,50)
orange = (255,165,0)
display_width = 500
display_height = 500
cell_size = 10
view_window_size = 2

def display_snake(snake_position, display):
    for position in snake_position[1:]:
        pygame.draw.rect(display, yellow, pygame.Rect(position[0], position[1], 10, 10))
    pygame.draw.rect(display, orange, pygame.Rect(snake_position[0][0], snake_position[0][1], 10, 10))

def display_apple(apple_position, display):
    pygame.draw.rect(display, green, pygame.Rect(apple_position[0], apple_position[1], 10, 10))

def starting_positions():
    score = random.randrange(4, 25)
    snake_start_x = random.randrange(score, 50) * 10
    snake_start_y = random.randrange(1, 50) * 10
    snake_start = [snake_start_x, snake_start_y]
    snake_position = []
    for i in range(score):
        snake_position.append([snake_start_x-(i)*10, snake_start_y])

    apple_position = [random.randrange(1, 50) * 10, random.randrange(1, 50) * 10]

    while apple_position in snake_position:
        apple_position = [random.randrange(1, 50) * 10, random.randrange(1, 50) * 10]

    return snake_start, snake_position, apple_position, score

def apple_distance_from_snake(apple_position, snake_position):
    return np.linalg.norm(np.array(apple_position) - np.array(snake_position))

def move_snake_start(button_direction, x, y):
    if button_direction == 1:
        x += 10
    elif button_direction == 0:
        x -= 10
    elif button_direction == 2:
        y += 10
    else:
        y -= 10
    return x, y

def generate_snake(snake_start, snake_position, apple_position, button_direction, score):
    new_x, new_y = move_snake_start(button_direction, snake_start[0], snake_start[1])
    snake_start[0] = new_x
    snake_start[1] = new_y

    if snake_start == apple_position:
        apple_position, score = collision_with_apple(apple_position, score)
        snake_position.insert(0, list(snake_start))

    else:
        snake_position.insert(0, list(snake_start))
        snake_position.pop()

    return snake_position, apple_position, score

def collision_with_apple(apple_position, score):
    apple_position = [random.randrange(1, 50) * 10, random.randrange(1, 50) * 10]
    score += 1
    return apple_position, score


def collision_with_boundaries(snake_start):
    if snake_start[0] >= display_width or snake_start[0] < 0 or snake_start[1] >= display_height or snake_start[1] < 0:
        return 1
    else:
        return 0


def collision_with_self(snake_start, snake_position):
    if snake_start in snake_position[1:]:
        return 1
    else:
        return 0


def blocked_directions(snake_position):
    current_direction_vector = np.array(snake_position[0]) - np.array(snake_position[1])

    left_direction_vector = np.array([current_direction_vector[1], -current_direction_vector[0]])
    right_direction_vector = np.array([-current_direction_vector[1], current_direction_vector[0]])

    is_front_blocked = is_direction_blocked(snake_position[0], snake_position, current_direction_vector)
    is_left_blocked = is_direction_blocked(snake_position[0], snake_position, left_direction_vector)
    is_right_blocked = is_direction_blocked(snake_position[0], snake_position, right_direction_vector)

    return current_direction_vector, is_front_blocked, is_left_blocked, is_right_blocked


def is_direction_blocked(start, snake_position, current_direction_vector):
    next_step = start + current_direction_vector
    if collision_with_boundaries(next_step) == 1 or collision_with_self(next_step.tolist(), snake_position) == 1:
        return 1
    else:
        return 0

def generate_random_direction(snake_position):

    direction = random.randint(-1, 1)

    return direction, direction_vector(snake_position, direction)

def direction_vector(snake_position, direction):
    current_direction_vector = np.array(snake_position[0]) - np.array(snake_position[1])
    left_direction_vector = np.array([current_direction_vector[1], -current_direction_vector[0]])
    right_direction_vector = np.array([-current_direction_vector[1], current_direction_vector[0]])

    new_direction = current_direction_vector

    if direction == -1:
        new_direction = left_direction_vector
    if direction == 1:
        new_direction = right_direction_vector

    button_direction = generate_button_direction(new_direction)

    return button_direction

def generate_button_direction(new_direction):
    button_direction = 3
    if new_direction.tolist() == [10, 0]:
        button_direction = 1
    if new_direction.tolist() == [-10, 0]:
        button_direction = 0
    if new_direction.tolist() == [0, 10]:
        button_direction = 2

    return button_direction

def play_game(snake_start, snake_position, apple_position, button_direction, \
                    score, display, clock, show=True, game_sleep=1):

    if show:
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                return True, snake_position, apple_position, score

        display.fill(white)

        display_apple(apple_position, display)
        display_snake(snake_position, display)

        snake_position, apple_position, score = generate_snake(snake_start, \
                    snake_position, apple_position, button_direction, score)

        pygame.display.set_caption("SCORE: " + str(score))
        pygame.display.update()
        clock.tick(game_sleep)
    else:
        snake_position, apple_position, score = generate_snake(snake_start, \
                    snake_position, apple_position, button_direction, score)

    return False, snake_position, apple_position, score
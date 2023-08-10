from game import *
from tqdm import tqdm
import asyncio
import os.path

async def generate_training_data(model1, model2, display, clock, show_game=True):

    if os.path.isfile(os.path.dirname(__file__) + '\\' + 'model1.h5'):
        model1.load_weights(os.path.dirname(__file__) + '\\' + 'model1.h5')
    if os.path.isfile(os.path.dirname(__file__) + '\\' + 'model2.h5'):
        model2.load_weights(os.path.dirname(__file__) + '\\' + 'model2.h5')

    training_data_x = []
    training_data_y = []
    training_data_mta_x = []
    training_data2_y = []
    input_training_data = []
    training_games = 1000
    steps_per_game = 2000
    steps_count = 0
    task1 = None
    task2 = None
    model_train1_step = 50
    model_train1_input_limit = 5000
    model_train2_step = 50
    model_train2_input_limit = 5000
    game_speed = 10

    for _ in tqdm(range(training_games)):
        snake_start, snake_position, apple_position, score = starting_positions()

        for _ in range(steps_per_game):

            quit_game, collision, direction, snake_position, apple_position, score, input_training_data = await predict_and_play( \
                training_data_x, training_data_mta_x, model1, model2, \
                snake_position, apple_position, snake_start, score, display, clock, show_game, game_speed, \
                training_data2_y, training_data_y, input_training_data, \
                steps_count)

            steps_count += 1

            if steps_count % model_train2_step == 0:
                if task2 != None:
                    await task2
                task2 = asyncio.create_task(train_model(model2, training_data_mta_x, training_data2_y, 'model2.h5'))
                if len(training_data2_y) >= model_train2_input_limit:
                    for j in range(model_train2_step):
                        training_data_mta_x.pop(0)
                        training_data2_y.pop(0)

            if steps_count % model_train1_step == 0:
                if task1 != None:
                    await task1
                task1 = asyncio.create_task(train_model(model1, training_data_x, training_data_y, 'model1.h5'))
                if len(training_data_y) >= model_train1_input_limit:
                    for j in range(model_train1_step):
                        training_data_x.pop(0)
                        training_data_y.pop(0)

            if quit_game:
                await train_model(model2, training_data_mta_x, training_data2_y, 'model2.h5')
                await train_model(model1, training_data_x, training_data_y, 'model1.h5')
                return

            if _ == steps_per_game - 1:
                print('\nExit game after 2000 steps!')

            if collision:
                print('\nExit game with collision! Direction: ', direction)
                break

    await train_model(model2, training_data_mta_x,training_data2_y, 'model2.h5')
    await train_model(model1, training_data_x, training_data_y, 'model1.h5')

    return

async def predict_and_play(training_data_x, training_data_mta_x, model1, model2, snake_position, apple_position,
                        snake_start, score, display, clock, show_game, game_speed, training_data2_y, training_data_y,
                        input_training_data, steps_count):

    skip_wrong_direction = False
    skip_apple_direction = False

    input_training_data_len = len(input_training_data)
    if skip_wrong_direction:
        input_training_data_len = 0
    training_data_mta_len = len(training_data_mta_x)
    if skip_apple_direction:
        training_data_mta_len = 0
    if input_training_data_len > 0:
        # predicted_wrong_direction = np.argmax(model1.predict(np.array(input_training_data))) - 1
        predicted_wrong_direction = get_predicted_wrong_direction(model1, input_training_data)
    else:
        predicted_wrong_direction = []
    if training_data_mta_len > 0:
        predicted_direction_to_apple_array = model2.predict(np.array([training_data_mta_x[-1]]))
        predicted_direction_to_apple = np.argmax(predicted_direction_to_apple_array[0])
    else:
        predicted_direction_to_apple_array = []
        predicted_direction_to_apple = None
    direction, button_direction = get_predicted_direction(predicted_wrong_direction, predicted_direction_to_apple_array, snake_position)
    print('input_training_data:', input_training_data)
    print('predicted_wrong_direction:', predicted_wrong_direction)
    print('predicted_direction_to_apple: ', predicted_direction_to_apple)
    print('result direction:', direction)
    print('result absolute direction:', button_direction)
    print('steps:', steps_count)

    current_direction_vector, is_front_blocked, is_left_blocked, is_right_blocked = blocked_directions(
        snake_position)

    prev_apple_position = [apple_position[0], apple_position[1]]
    prev_snake_position = snake_position
    quit_game, snake_position, apple_position, score = play_game(snake_start, \
                snake_position, apple_position, button_direction, score, \
                display, clock, show_game, game_speed)
    collision = (is_front_blocked == 1 and direction == 0) or \
            (is_left_blocked == 1 and direction == -1) or \
            (is_right_blocked == 1 and direction == 1)

    x, y, input_training_data = get_training_data_ex3(prev_snake_position, is_front_blocked, is_left_blocked, is_right_blocked)

    if skip_wrong_direction:
        x = []
        y = []

    x2, y2 = get_training_data_move_to_apple(button_direction, snake_position, prev_apple_position)

    if skip_apple_direction:
        y2 = None

    if y2 != None and prev_apple_position == apple_position:
        training_data_mta_x.append(x2)
        training_data2_y.append(y2)

    for item in x:
        training_data_x.append(item)
    for item in y:
        training_data_y.append(item)
    
    return quit_game, collision, direction, snake_position, apple_position, score, input_training_data

async def train_model(model,training_data_x,training_data_y,save_to_file_name):
    if len(training_data_x) == 0:
        return
    npx = np.array(training_data_x)
    npy = np.array(training_data_y)
    model.fit(npx,npy, batch_size = 256, epochs = 3, verbose = 1)
    model.save_weights(os.path.dirname(__file__) + '\\' + save_to_file_name)
    print('\nModel trainded and saved')

def get_index(position):
    return int(position/cell_size)

def get_training_data_move_to_apple(button_direction, snake_position, apple_position):
    prev_snake = snake_position[1]
    curr_dist = apple_distance_from_snake(apple_position, snake_position[0])
    prev_dist = apple_distance_from_snake(apple_position, prev_snake)

    while curr_dist >= prev_dist:
        i = random.randint(0, 3)
        new_x, new_y = move_snake_start(i, prev_snake[0], prev_snake[1])
        curr_snake = [new_x, new_y]
        curr_dist = apple_distance_from_snake(apple_position, curr_snake)
        button_direction = i

    sx = prev_snake[0] / display_width
    sy = prev_snake[1] / display_height
    ax = apple_position[0] / display_width
    ay = apple_position[1] / display_height

    x_to_add = [sx, sy, ax, ay]
    y_to_add = button_direction

    return x_to_add, y_to_add
    
def get_training_data_ex3(snake_position, is_front_blocked, is_left_blocked, is_right_blocked):
    x_to_add = []
    y_to_add = []
    input_data = [[],[],[]]
    directions = []

    forward_direction_vector = np.array(snake_position[0]) - np.array(snake_position[1])
    left_direction_vector = np.array([forward_direction_vector[1], -forward_direction_vector[0]])
    right_direction_vector = np.array([-forward_direction_vector[1], forward_direction_vector[0]])

    left_first_conner = snake_position[0] + left_direction_vector - forward_direction_vector - forward_direction_vector
    left_second_conner = left_first_conner + left_direction_vector - forward_direction_vector

    num = 0
    directions.append([num, left_first_conner, forward_direction_vector])
    directions.append([num, left_second_conner, forward_direction_vector])

    left_first_conner = snake_position[0] + forward_direction_vector + left_direction_vector + left_direction_vector
    left_second_conner = left_first_conner + forward_direction_vector + left_direction_vector

    num += 1
    directions.append([num, left_first_conner, right_direction_vector])
    directions.append([num, left_second_conner, right_direction_vector])

    left_first_conner = snake_position[0] + right_direction_vector + forward_direction_vector + forward_direction_vector
    left_second_conner = left_first_conner + right_direction_vector + forward_direction_vector

    num += 1
    directions.append([num, left_first_conner, -forward_direction_vector])
    directions.append([num, left_second_conner, -forward_direction_vector])

    for item in directions:
        current = item[1]
        right_direction = item[2]
        blocked_count = 0
        for i in range(3):
            if is_direction_blocked(current, snake_position, right_direction):
                blocked_count += 1
            current += right_direction
        input_data[item[0]].append(blocked_count)
        
    directions = [is_left_blocked, is_front_blocked, is_right_blocked]
    if True in directions:
        for i in range(3):
            x_to_add.append(input_data[i])
            if directions[i] or input_data[i][0] > 1 or input_data[i][1] > 1 :
                y_to_add.append(1)
            else:
                y_to_add.append(0)

    return x_to_add, y_to_add, input_data
    
def get_training_data_ex2(snake_position):

    closest_left = []
    closest_right = []
    closest_forward = []
    x_to_add = []
    y_to_add = []
    
    forward_direction_vector = np.array(snake_position[0]) - np.array(snake_position[1])
    left_direction_vector = np.array([forward_direction_vector[1], -forward_direction_vector[0]])
    right_direction_vector = np.array([-forward_direction_vector[1], forward_direction_vector[0]])

    directions = [[left_direction_vector, 0, []], [forward_direction_vector, 0, []], [right_direction_vector, 0, []]]

    for i in range(3):
        current = [snake_position[0][0], snake_position[0][1]]
        is_blocked = False
        step = 0
        current_direction = directions[i][0]
        while is_blocked == False:
            is_blocked = is_direction_blocked(current, snake_position, current_direction)
            step += 1
            current += current_direction
        directions[i][1] = step
        directions[i][2] = current

    dir_data = directions[0][2]
    closest_left = [dir_data[0] / display_width, dir_data[1] / display_height]
    dir_data = directions[1][2]
    closest_forward = [dir_data[0] / display_width, dir_data[1] / display_height]
    dir_data = directions[2][2]
    closest_right = [dir_data[0] / display_width, dir_data[1] / display_height]
    dir_data = snake_position[1]
    snake_prev = [dir_data[0] / display_width, dir_data[1] / display_height]
    dir_data = snake_position[0]
    snake_curr = [dir_data[0] / display_width, dir_data[1] / display_height]

    input_data = np.concatenate((closest_left, closest_forward, closest_right, snake_prev, snake_curr))
    input_data = input_data.reshape(-1)
    
    for i in range(3):
        if directions[i][1] == 1:
            x_to_add.append(input_data)
            y_to_add.append(i)

    return x_to_add, y_to_add, [input_data]

def get_training_data_ex(snake_position, is_front_blocked, is_left_blocked, is_right_blocked):
    size = view_window_size * 2
    map1 = []
    map2 = []
    map3 = []
    map4 = []
    current_direction_vector = np.array(snake_position[0]) - np.array(snake_position[1])
    current_direction_vector = current_direction_vector/cell_size
    current_direction_vector = [int(item) for item in current_direction_vector]
    first_axis = 1
    second_axis = 0
    if current_direction_vector[0] == 0:
        first_axis = 0
        second_axis = 1
    x_shift = -1
    if current_direction_vector[second_axis] > 0:
        x_shift = 1
    if current_direction_vector[0] == 0:
        y_shift = current_direction_vector[second_axis]
    else:
        y_shift = -current_direction_vector[second_axis]
    first_shift = y_shift
    second_shift = x_shift
    if current_direction_vector[0] == 0:
        first_shift = x_shift
        second_shift = y_shift
    # first_start = snake_position[0][first_axis] + first_shift * view_window_size
    # first_end = snake_position[0][first_axis] - first_shift * view_window_size
    # second_start = snake_position[0][second_axis] + second_shift * view_window_size
    # second_end = snake_position[0][second_axis] + second_shift * view_window_size
    for j in range(size+1):
        for i in range(size+1):
            ii = snake_position[0][first_axis] + cell_size * (view_window_size * first_shift - i * first_shift)
            jj = snake_position[0][second_axis] + cell_size * (view_window_size * second_shift - j * second_shift)
            point = [0, 0]
            point[first_axis] = ii
            point[second_axis] = jj
            if point in snake_position or ii < 0 or jj < 0 or ii >= display_width or jj >= display_height :
                map1.append([item/display_width for item in point])
                map4.append([0.0, 0.0])
            else:
                map2.append([item/display_width for item in point])
                map3.append([0.0, 0.0])
    x_to_add = []
    y_to_add = []
    input_data = np.concatenate((map1, map3, map2, map4)).reshape(-1)
    
    for i in range(3):
        direction = i - 1
        if (is_front_blocked == 1 and direction == 0) or \
                    (is_left_blocked == 1 and direction == -1) or \
                    (is_right_blocked == 1 and direction == 1):
            x_to_add.append(input_data)
            y_to_add.append(i)

    return x_to_add, y_to_add, [input_data]

def get_training_data(snake_position, is_front_blocked, is_left_blocked, is_right_blocked):
    snake_head_mark = 0.2
    snake_body_mark = 0.5
    apple_mark = 0.7
    map = np.full((int(display_width/cell_size), int(display_height/cell_size)), 0.0)
    # map = np.full((int(display_width/cell_size), int(display_height/cell_size), 1), 0.0)
    for i in snake_position[1:]:
        x = get_index(i[0])
        y = get_index(i[1])
        # map[x,y,0] = snake_body_mark
        map[x,y] = snake_body_mark
    x = get_index(snake_position[0][0])
    y = get_index(snake_position[0][1])
    # map[x,y,0] = snake_head_mark
    map[x,y] = snake_head_mark

    input_data = map.reshape(-1)
    x_to_add = []
    y_to_add = []
    
    for i in range(3):
        direction = i - 1
        if (is_front_blocked == 1 and direction == 0) or \
                    (is_left_blocked == 1 and direction == -1) or \
                    (is_right_blocked == 1 and direction == 1):
            x_to_add.append(input_data)
            y_to_add.append(i)
    
    return x_to_add, y_to_add, [input_data]

def get_direction(button_direction, snake_position):
    for i in range(3):
        tmp_direction = i-1
        tmp_button_direction = direction_vector(snake_position, tmp_direction)
        if button_direction == tmp_button_direction:
            return tmp_direction
    return None


def get_predicted_direction(wrong_direction, direction_to_apple_array, snake_position):

    if len(direction_to_apple_array) == 0 and len(wrong_direction) == 0:
        return generate_random_direction(snake_position)

    if len(direction_to_apple_array) == 0:
        direction = random.randint(-1, 1)
    else:
        button_direction = np.argmax(direction_to_apple_array[0])
        direction = get_direction(button_direction, snake_position)
        while direction == None and len(direction_to_apple_array) > 1:
            direction_to_apple_array = np.delete(direction_to_apple_array[0], button_direction, 0)
            button_direction = np.argmax(direction_to_apple_array)
            direction = get_direction(button_direction, snake_position)
    
    if direction == None:
        direction = random.randint(-1, 1)
    button_direction = direction_vector(snake_position, direction)

    if len(wrong_direction) != 0:
        z = 0
        while wrong_direction[direction+1] and z<10:
            direction = random.randint(-1, 1)
            button_direction = direction_vector(snake_position, direction)
            z += 1

    return direction, button_direction

def get_predicted_wrong_direction(model, input):
    result = model.predict(np.array(input))
    print('result:', result)
    result = result > 0.5
    result = np.reshape(result, -1)

    return result
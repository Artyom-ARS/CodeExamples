from game import *
from nn_behavior import generate_training_data

from keras.models import Sequential
from keras.layers import Dense
from keras.layers import Conv2D
from keras.layers import MaxPool2D
from keras.layers import Flatten
from keras.layers import Dropout
import asyncio

import sys


show_game = True
display = None
clock = None

if len(sys.argv) > 1:
    show_game = sys.argv[1] == 'True'

if show_game:
    pygame.init()
    display=pygame.display.set_mode((display_width,display_height))
    clock=pygame.time.Clock()

'''
LEFT -> button_direction = 0
RIGHT -> button_direction = 1
DOWN ->button_direction = 2
UP -> button_direction = 3
'''
'''
LEFT -> direction = -1
FORWARD -> direction = 0
RIGHT ->button_direction = 1
'''

# Snake prevent collisions model
model1 = Sequential()
# model1.add(Conv2D(250, (3,3), activation='relu', kernel_initializer='he_uniform', \
#     input_shape=(int(display_width/cell_size), int(display_height/cell_size), 1)))
# model1.add(MaxPool2D((2, 2)))
# model1.add(Flatten())
# model1.add(Dense(20, activation='relu', kernel_initializer='he_uniform'))
# model1.add(Dropout(0.3))
# model1.add(Dense(3, activation='softmax'))
# model1.add(Dense(250, input_dim=(view_window_size*2+1)*(view_window_size*2+1)*2*2, activation='relu'))
# model1.add(Dense(50, activation='relu'))
# model1.add(Dense(4,  activation='softmax'))
# model1.compile(optimizer='adam', loss='sparse_categorical_crossentropy', metrics=['accuracy'])
model1.add(Dense(20, input_dim=2, kernel_initializer='he_normal', activation='relu'))
model1.add(Dense(1,  activation='sigmoid'))
model1.compile(optimizer='adam', loss='binary_crossentropy', metrics=['accuracy'])

# Snake move to apple model
model2 = Sequential()
model2.add(Dense(64, input_dim=4, activation='relu'))
model2.add(Dense(16, activation='relu'))
model1.add(Dropout(0.3))
model2.add(Dense(4,  activation='softmax'))

model2.compile(optimizer='adam', loss='sparse_categorical_crossentropy', metrics=['accuracy'])

asyncio.run(generate_training_data(model1, model2, display,clock, show_game))

# model.fit((np.array(training_data_x).reshape(-1,7)),( np.array(training_data_y).reshape(-1,3)), batch_size = 256,epochs= 3)
#
# model.save_weights('model.h5')
# model_json = model.to_json()
# with open('model.json', 'w') as json_file:
#     json_file.write(model_json)
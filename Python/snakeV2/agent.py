import tensorflow as tf
import numpy as np
import base64, io, time
import os

class PgAgent:
  def __init__(self, verbose = 0):
    self.verbose = verbose
    if self.verbose == 1:
      print('PgAgent init')
    self.gamma=0.85

  def init_model(self, model, n_actions): 
    if self.verbose == 1:
      print('PgAgent set_model')
    self.model = model
    self.n_actions = n_actions
    
  # Function that takes observations as input, executes a forward pass through model, 
  #   and outputs a sampled action.
  # Arguments:
  #   observation: observation which is fed as input to the model
  # Returns:
  #   action: choice of agent action
  def choose_action(self, observation):
    observation = np.expand_dims(observation, axis=0)
    logits = self.model.predict(observation)
    prob_weights = tf.nn.softmax(logits).numpy()
    action = np.random.choice(self.n_actions, size=1, p=prob_weights.flatten())[0]

    return action
    
  # Compute normalized, discounted, cumulative rewards (i.e., return)
  # Arguments:
  #   rewards: reward at timesteps in episode
  # Returns:
  #   normalized discounted reward
  def discount_rewards(self, rewards): 
    discounted_rewards = np.zeros_like(rewards)
    R = 0
    for t in reversed(range(0, len(rewards))):
      if rewards[t] != 0.0:
        R = 0.0
      R = R * self.gamma + rewards[t]
      discounted_rewards[t] = R
        
    return self.__normalize(discounted_rewards)

  # Helper function that normalizes an np.array x
  def __normalize(self, x):
    # print('normalize x', x)
    x -= np.mean(x)
    std = np.std(x)
    if std == 0.0:
      std = 1.0
    x /= std
    return x.astype(np.float32)
    
  # Arguments:
  #   logits: network's predictions for actions to take
  #   actions: the actions the agent took in an episode
  #   rewards: the rewards the agent received in an episode
  # Returns:
  #   loss
  def __compute_loss(self, logits, actions, rewards): 
    neg_logprob = tf.nn.sparse_softmax_cross_entropy_with_logits(logits=logits, labels=actions)
    
    loss = tf.reduce_mean( neg_logprob * rewards )
    return loss
    
  def train_step(self, optimizer, observations, actions, discounted_rewards):
    with tf.GradientTape() as tape:
      observations = np.array(observations)
      logits = self.model(observations)

      loss = self.__compute_loss(logits, actions, discounted_rewards)

    grads = tape.gradient(loss, self.model.trainable_variables)
    optimizer.apply_gradients(zip(grads, self.model.trainable_variables))
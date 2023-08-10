import trainer.task as tr
import atari_py
import os.path

print(atari_py.list_games())
args = tr.init()
args.restore = True
args.render = True
args.output_dir = os.path.join(os.path.dirname(__file__), 'tmp', 'pong_output')
args.n_epoch = 5
args.max_to_keep = args.n_epoch // args.save_checkpoint_steps
print(os.path.dirname(__file__))
print(args.output_dir)
print('==========================START===========')
tr.main(args)
print('Enter your name:')
name = input()
try:
    if not isinstance(name, str):
        raise ValueError('Name must be a string.')
    print(f'Hello, {name}!')
except ValueError as e:
    print(e)
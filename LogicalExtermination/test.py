def count_fruits_starting_with_a(fruits_list):
    count = 0
    for fruit in fruits_list:
        if fruit.startswith('a'):
            count += 1
    return count

fruits = ['apple', 'banana', 'cherry']
result = count_fruits_starting_with_a(fruits)
print(result)
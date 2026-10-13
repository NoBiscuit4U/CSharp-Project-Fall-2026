def find_most_frequent_fruits(fruits):
    fruit_count = {}
    max_count = 0

    for fruit in fruits:
        if fruit in fruit_count:
            fruit_count[fruit] += 1
        else:
            fruit_count[fruit] = 1
        if fruit_count[fruit] > max_count:
            max_count = fruit_count[fruit]

    most_frequent = [fruit for fruit, count in fruit_count.items() if count == max_count]
    most_frequent.sort()
    return most_frequent

input_list = ['apple', 'banana', 'cherry']
result = find_most_frequent_fruits(input_list)
print(result)
input = [8, 8, 6]
numbers = input
total = 0
for v in numbers:
    total += v
n = len(numbers)
avg = total / n if n != 0 else 0.0
count = 0
for v in numbers:
    if v > avg:
        count += 1
avg_rounded = round(avg, 2)
print("Average: {:.2f}, Count above average: {}".format(avg_rounded, count))
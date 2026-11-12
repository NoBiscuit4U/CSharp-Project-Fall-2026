n = 8
sum_of_evens = 0
for i in range(1, n + 1):
    if i % 2 == 0:
        sum_of_evens += i
print('The total sum of even numbers from 1 to', n, 'is:', sum_of_evens)
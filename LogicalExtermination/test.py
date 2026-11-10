scores = [72, 85, 91, 64, 78]
# Calculate the average score
average_score = sum(scores) / len(scores)
# Count how many scores are above the average
above_average_count = sum(1 for score in scores if score > average_score)
# Print the results
print(f'Average Score: {average_score:.2f}, Scores Above Average: {above_average_count}')
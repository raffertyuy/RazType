# Fibonacci series:
# the sum of two elements defines the next
a, b = 0, 1

while a < 100:
  print a
  a, b = b, a+b

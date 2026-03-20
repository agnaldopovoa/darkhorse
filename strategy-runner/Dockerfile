FROM python:3.11-alpine

# Set non-interactive mode
ENV PYTHONUNBUFFERED=1
ENV PYTHONDONTWRITEBYTECODE=1

# Create a non-root user for security
RUN adduser -D -u 1000 runner
WORKDIR /app

# Install basic dependencies (pandas/numpy usually needed for strats, use precompiled wheels in alpine via apk if possible to save time)
# For this MVP, we will only install requests. In a real scenario, pandas/numpy via pip or apk add py3-numpy.
RUN pip install --no-cache-dir requests

# Copy runner scripts
COPY runner.py /app/
COPY backtest_runner.py /app/

# Restrict permissions
RUN chown -R root:root /app && \
    chmod 755 /app && \
    chmod 555 /app/runner.py /app/backtest_runner.py

# Switch to non-root
USER runner

# Ensure runner.py is the default entrypoint
ENTRYPOINT ["python3", "/app/runner.py"]

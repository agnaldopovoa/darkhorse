import sys
import json
import traceback

class DataWrapper:
    def __init__(self, data_list):
        # Allow access to OHLCV data.
        # data_list is expected to be a list of dicts: [{'open': 100, 'high': 102, ...}, ...]
        self._data = data_list

    @property
    def close(self):
        if not self._data: return 0.0
        return self._data[-1].get('close', 0.0)

    @property
    def open(self):
        if not self._data: return 0.0
        return self._data[-1].get('open', 0.0)
    
    @property
    def high(self):
        if not self._data: return 0.0
        return self._data[-1].get('high', 0.0)

    @property
    def low(self):
        if not self._data: return 0.0
        return self._data[-1].get('low', 0.0)

    @property
    def volume(self):
        if not self._data: return 0.0
        return self._data[-1].get('volume', 0.0)
        
    def get_history(self):
        return self._data

class Context:
    def __init__(self, params, balance):
        self.parameters = params
        self.balance = balance

def run_strategy(context_payload):
    try:
        # 1. Parse payload
        script_code = context_payload.get("script", "")
        ohlcv = context_payload.get("ohlcv", [])
        balance = context_payload.get("balance", {})
        params = context_payload.get("parameters", {})
        
        # 2. Setup execution environment
        data_obj = DataWrapper(ohlcv)
        ctx_obj = Context(params, balance)
        
        # Restricted globals for eval/exec
        restricted_globals = {
            "__builtins__": __builtins__,
        }
        
        # 3. Compile and execute user script to define init/tick
        exec_locals = {}
        exec(script_code, restricted_globals, exec_locals)
        
        # 4. Call tick function
        if "tick" not in exec_locals:
            raise Exception("Script must define a 'tick(context, data)' function.")
            
        signal = exec_locals["tick"](ctx_obj, data_obj)
        quantity = 0.0 # MVP simplificaton. Real implementation sets sizing logic
        
        # 5. Output Result
        result = {
            "signal": signal,
            "quantity": quantity,
            "reason": "OK"
        }
        print(json.dumps(result))

    except Exception as e:
        error_result = {
            "signal": "ERROR",
            "quantity": 0.0,
            "reason": str(e),
            "traceback": traceback.format_exc()
        }
        print(json.dumps(error_result))

if __name__ == "__main__":
    # Read entire stdin
    input_data = sys.stdin.read()
    if not input_data.strip():
        print(json.dumps({"signal": "ERROR", "reason": "No input provided"}))
        sys.exit(1)
        
    try:
        payload = json.loads(input_data)
        run_strategy(payload)
    except json.JSONDecodeError:
        print(json.dumps({"signal": "ERROR", "reason": "Invalid JSON input"}))
        sys.exit(1)

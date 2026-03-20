import sys
import json
import traceback

# Simplified backtest runner that iteratively feeds OHLCV slice by slice
def run_backtest(payload):
    try:
        script_code = payload.get("script", "")
        ohlcv = payload.get("ohlcv", [])
        params = payload.get("parameters", {})
        initial_balance = payload.get("initialBalance", 1000.0)
        
        restricted_globals = {"__builtins__": __builtins__}
        exec_locals = {}
        exec(script_code, restricted_globals, exec_locals)
        
        if "tick" not in exec_locals:
            raise Exception("Script must define a 'tick(context, data)' function.")
            
        tick_func = exec_locals["tick"]
        
        trades = []
        balance = initial_balance
        asset_qty = 0.0
        
        # Iterate over history simulating step-by-step
        for i in range(1, len(ohlcv) + 1):
            slice_data = ohlcv[:i]
            
            # Simulated data and context
            class DataMock:
                _data = slice_data
                @property
                def close(self): return self._data[-1].get('close', 0.0)
                @property
                def open(self): return self._data[-1].get('open', 0.0)
            
            class CtxMock:
                parameters = params
                balance = {"USDT": balance, "ASSET": asset_qty}
                
            signal = tick_func(CtxMock(), DataMock())
            current_price = slice_data[-1].get('close', 0.0)
            
            # Process simple market orders on signal
            if signal == "BUY" and balance > 0:
                qty = balance / current_price
                asset_qty += qty
                balance = 0
                trades.append({"type": "BUY", "price": current_price, "index": i-1})
            elif signal == "SELL" and asset_qty > 0:
                balance += asset_qty * current_price
                asset_qty = 0
                trades.append({"type": "SELL", "price": current_price, "index": i-1})
                
        final_value = balance + (asset_qty * ohlcv[-1].get('close', 0.0) if ohlcv else 0)
        pnl = final_value - initial_balance
        
        print(json.dumps({
            "status": "COMPLETED",
            "pnl": pnl,
            "totalTrades": len(trades),
            "finalValue": final_value,
            "trades": trades
        }))
        
    except Exception as e:
        print(json.dumps({"status": "FAILED", "error": str(e), "traceback": traceback.format_exc()}))

if __name__ == "__main__":
    input_data = sys.stdin.read()
    if input_data.strip():
        try:
            payload = json.loads(input_data)
            run_backtest(payload)
        except:
            sys.exit(1)

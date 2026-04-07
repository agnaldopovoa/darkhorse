import { Key, Plus, Trash2, X } from 'lucide-react';
import { useState, useEffect } from 'react';
import { brokersApi } from '../api/brokersApi';
import type { CreateBrokerDto } from '../api/brokersApi';
import type { BrokerCredentialDto } from '../api/models';

export default function Brokers() {
  const [brokers, setBrokers] = useState<BrokerCredentialDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [showAddForm, setShowAddForm] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const [formData, setFormData] = useState<CreateBrokerDto>({
    brokerName: '',
    apiKey: '',
    secret: '',
    feeRate: 0.001,
    fundingRate: 0.0001,
    isSandbox: false
  });

  const fetchBrokers = async () => {
    try {
      const data = await brokersApi.getAll();
      setBrokers(data);
    } catch (err) {
      console.error('Failed to fetch brokers', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchBrokers();
  }, []);

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this broker credential?')) return;
    try {
      await brokersApi.remove(id);
      setBrokers(prev => prev.filter(b => b.id !== id));
    } catch (err) {
      console.error('Failed to delete broker', err);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await brokersApi.create(formData);
      setShowAddForm(false);
      setFormData({
        brokerName: '', apiKey: '', secret: '', feeRate: 0.001, fundingRate: 0.0001, isSandbox: false
      });
      fetchBrokers();
    } catch (err) {
      console.error('Failed to create broker', err);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-white">Broker Credentials</h1>
        {!showAddForm && (
          <button onClick={() => setShowAddForm(true)} className="btn btn-primary gap-2">
            <Plus className="h-4 w-4" />
            Add Broker
          </button>
        )}
      </div>

      {showAddForm && (
        <div className="card p-6 border-primary/30 bg-primary/5">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-lg font-semibold text-white">Add New Broker</h2>
            <button onClick={() => setShowAddForm(false)} className="text-muted hover:text-white">
              <X className="h-5 w-5" />
            </button>
          </div>
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-text">Broker Name</label>
                <input
                  required
                  type="text"
                  className="input"
                  placeholder="e.g. Binance Main"
                  value={formData.brokerName}
                  onChange={e => setFormData({...formData, brokerName: e.target.value})}
                />
              </div>
              <div className="flex flex-col gap-1.5 justify-end">
                <label className="flex items-center gap-2 text-sm text-text cursor-pointer py-2 px-1">
                  <input
                    type="checkbox"
                    checked={formData.isSandbox}
                    onChange={e => setFormData({...formData, isSandbox: e.target.checked})}
                    className="rounded border-border bg-background focus:ring-primary/20 text-primary"
                  />
                  <span>Sandbox / Testnet mode</span>
                </label>
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-text">API Key</label>
                <input
                  required
                  type="text"
                  className="input"
                  value={formData.apiKey}
                  onChange={e => setFormData({...formData, apiKey: e.target.value})}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-text">Secret Key</label>
                <input
                  required
                  type="password"
                  className="input"
                  value={formData.secret}
                  onChange={e => setFormData({...formData, secret: e.target.value})}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-text">Fee Rate (%)</label>
                <input
                  required
                  type="number"
                  step="0.001"
                  className="input"
                  value={formData.feeRate}
                  onChange={e => setFormData({...formData, feeRate: parseFloat(e.target.value)})}
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-text">Funding Rate (%)</label>
                <input
                  required
                  type="number"
                  step="0.0001"
                  className="input"
                  value={formData.fundingRate}
                  onChange={e => setFormData({...formData, fundingRate: parseFloat(e.target.value)})}
                />
              </div>
            </div>
            <div className="flex justify-end gap-3 mt-4 pt-4 border-t border-border">
              <button type="button" onClick={() => setShowAddForm(false)} className="btn border border-border bg-transparent hover:bg-white/5 text-text">
                Cancel
              </button>
              <button type="submit" disabled={submitting} className="btn btn-primary min-w-[120px]">
                {submitting ? 'Adding...' : 'Save Credential'}
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
           <div className="h-32 bg-primary/10 rounded-lg animate-pulse"></div>
           <div className="h-32 bg-primary/10 rounded-lg animate-pulse"></div>
        </div>
      ) : brokers.length === 0 ? (
        <div className="card p-12 flex flex-col items-center justify-center text-center">
          <Key className="h-10 w-10 text-muted mb-4 opacity-50" />
          <h3 className="text-lg font-medium text-white mb-2">No Brokers Configured</h3>
          <p className="text-muted text-sm max-w-sm">Connect your exchange API keys to allow the trading engines to execute orders.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {brokers.map(broker => (
            <div key={broker.id} className="card p-6 group hover:border-primary/50 transition-all">
              <div className="flex justify-between items-start mb-4">
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-md bg-primary/10 text-primary">
                    <Key className="h-5 w-5" />
                  </div>
                  <div>
                    <h3 className="font-semibold text-white">{broker.brokerName}</h3>
                    <p className="text-xs text-muted">Status: <span className={broker.status === 'OK' ? 'text-success' : 'text-danger'}>{broker.status}</span></p>
                  </div>
                </div>
                <button 
                  onClick={() => handleDelete(broker.id)}
                  className="text-muted hover:text-danger opacity-0 group-hover:opacity-100 transition-opacity"
                  title="Delete Broker"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </div>
              <div className="text-xs text-muted mt-4 border-t border-border pt-4">
                <div className="flex justify-between"><span>Fee Rate:</span> <span className="text-text">{(broker.feeRate * 100).toFixed(3)}%</span></div>
                <div className="flex justify-between mt-1"><span>Mode:</span> <span className="text-primary font-medium">{broker.isSandbox ? 'Sandbox' : 'Live'}</span></div>
                <div className="flex justify-between mt-1"><span>Added:</span> <span>{new Date(broker.createdAt).toLocaleDateString()}</span></div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

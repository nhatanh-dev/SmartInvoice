import React from 'react';
import { Card } from 'antd';
import { Panel, PanelGroup, PanelResizeHandle } from 'react-resizable-panels';
import {
  BarChart, Bar, LineChart, Line, PieChart, Pie, Cell, AreaChart, Area,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts';
import type { MonthlyTrendItem, RiskTrendItem, StatusDistributionItem } from '../../services/dashboard';

interface AnalyticsChartsProps {
  monthlyTrends: MonthlyTrendItem[];
  riskTrends: RiskTrendItem[];
  statusDistribution: StatusDistributionItem[];
}

const tooltipStyle = {
  background: '#fff',
  border: '1px solid hsl(220, 15%, 88%)',
  borderRadius: 10,
  boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
  fontSize: 12,
};

const formatYAxis = (tickItem: number) => {
  if (tickItem >= 1000000000) return (tickItem / 1000000000).toFixed(1) + ' Tỷ';
  if (tickItem >= 1000000) return (tickItem / 1000000).toFixed(0) + ' Tr';
  if (tickItem >= 1000) return (tickItem / 1000).toFixed(0) + ' K';
  return tickItem.toString();
};

const formatCurrencyTooltip = (value: number) => {
  return value.toLocaleString('vi-VN') + ' ₫';
};

const ResizeHandle = () => (
  <PanelResizeHandle className="w-2 flex items-center justify-center group cursor-col-resize">
    <div className="w-1 h-8 rounded-full bg-gray-300 group-hover:bg-blue-400 transition-colors" />
  </PanelResizeHandle>
);

const AnalyticsCharts: React.FC<AnalyticsChartsProps> = ({ monthlyTrends, riskTrends, statusDistribution }) => {
  const displayMonthlyTrends = monthlyTrends;
  const displayRiskTrends = riskTrends;
  const displayStatus = statusDistribution;

  return (
    <div className="mt-4 flex flex-col gap-4">
      {/* Row 1: Bar Chart + Area Chart */}
      <PanelGroup direction="horizontal">
        <Panel defaultSize={50} minSize={25}>
          <div className="h-full pr-2">
            <Card variant="borderless" title="Hóa đơn theo tháng" style={{ borderRadius: 12, height: '100%' }}
              styles={{ body: { padding: '8px 16px 16px' } }}>
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={displayMonthlyTrends} barGap={2}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(220,15%,92%)" />
                  <XAxis dataKey="month" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis fontSize={12} tickLine={false} axisLine={false} />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="approved" name="Đã duyệt" fill="#2d9a5c" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="pending" name="Chờ duyệt" fill="#e6a817" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="rejected" name="Từ chối" fill="#d63031" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </Card>
          </div>
        </Panel>
        <ResizeHandle />
        <Panel defaultSize={50} minSize={25}>
          <div className="h-full pl-2">
            <Card variant="borderless" title="Xu hướng rủi ro (%)" style={{ borderRadius: 12, height: '100%' }}
              styles={{ body: { padding: '8px 16px 16px' } }}>
              <ResponsiveContainer width="100%" height={280}>
                <AreaChart data={displayRiskTrends}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(220,15%,92%)" />
                  <XAxis dataKey="month" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis fontSize={12} tickLine={false} axisLine={false} />
                  <Tooltip contentStyle={tooltipStyle} />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Area type="monotone" dataKey="green" name="An toàn" fill="#2d9a5c" fillOpacity={0.15} stroke="#2d9a5c" strokeWidth={2} />
                  <Area type="monotone" dataKey="yellow" name="Lưu ý" fill="#e6a817" fillOpacity={0.15} stroke="#e6a817" strokeWidth={2} />
                  <Area type="monotone" dataKey="orange" name="Cảnh báo" fill="#e17055" fillOpacity={0.15} stroke="#e17055" strokeWidth={2} />
                  <Area type="monotone" dataKey="red" name="Nguy hiểm" fill="#d63031" fillOpacity={0.15} stroke="#d63031" strokeWidth={2} />
                </AreaChart>
              </ResponsiveContainer>
            </Card>
          </div>
        </Panel>
      </PanelGroup>

      {/* Row 2: Pie Chart + DÒNG TIỀN THEO THÁNG */}
      <PanelGroup direction="horizontal">
        <Panel defaultSize={50} minSize={25}>
          <div className="h-full pr-2">
            <Card variant="borderless" title="Tỷ lệ trạng thái" style={{ borderRadius: 12, height: '100%' }}
              styles={{ body: { padding: '8px 16px 16px' } }}>
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie data={displayStatus} cx="50%" cy="50%" innerRadius={55} outerRadius={95}
                    paddingAngle={4} dataKey="value" label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                    labelLine={false} fontSize={11}>
                    {displayStatus.map((entry, i) => (
                      <Cell key={i} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip contentStyle={tooltipStyle} />
                </PieChart>
              </ResponsiveContainer>
            </Card>
          </div>
        </Panel>
        <ResizeHandle />
        <Panel defaultSize={50} minSize={25}>
          <div className="h-full pl-2">
            <Card variant="borderless" title="Dòng tiền theo tháng" style={{ borderRadius: 12, height: '100%' }}
              styles={{ body: { padding: '8px 16px 16px' } }}>
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={displayMonthlyTrends} barGap={0}>
                  {/* Ẩn bớt gạch dọc cho thoáng mắt */}
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(220,15%,92%)" vertical={false} /> 
                  <XAxis dataKey="month" fontSize={12} tickLine={false} axisLine={false} />
                  
                  {/* Dùng hàm formatYAxis để rút gọn số */}
                  <YAxis fontSize={12} tickLine={false} axisLine={false} tickFormatter={formatYAxis} width={65} />
                  
                  {/* Dùng hàm formatCurrencyTooltip để hiện số tiền đẹp khi di chuột vào */}
                  <Tooltip contentStyle={tooltipStyle} formatter={formatCurrencyTooltip} />
                  
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  
                  {/* Cột Tổng tiền (Xanh dương đậm) */}
                  <Bar dataKey="totalAmount" name="Tổng tiền HĐ" fill="#1a4b8c" radius={[4, 4, 0, 0]} />
                  
                  {/* Cột Tiền thuế (Xanh lá) */}
                  <Bar dataKey="totalTaxAmount" name="Tiền thuế" fill="#2db791" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </Card>
          </div>
        </Panel>
      </PanelGroup>
    </div>
  )
};

export default AnalyticsCharts;

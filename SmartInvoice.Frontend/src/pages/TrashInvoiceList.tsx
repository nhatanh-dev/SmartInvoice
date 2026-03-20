import React, { useState } from 'react';
import { Card, Table, Typography, Space, Button, Input, Modal, message, Tag } from 'antd';
import { RetweetOutlined, DeleteOutlined, ExclamationCircleOutlined, SearchOutlined } from '@ant-design/icons';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { invoiceService } from '../services/invoice';
import StatusBadge from '../components/ui/StatusBadge';

const { Title, Text } = Typography;

const TrashInvoiceList: React.FC = () => {
    const queryClient = useQueryClient();
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [keyword, setKeyword] = useState('');
    const [searchText, setSearchText] = useState('');

    const { data: invoiceData, isLoading } = useQuery({
        queryKey: ['trash-invoices', page, pageSize, keyword],
        queryFn: () => invoiceService.getTrashInvoices(page, pageSize, keyword),
    });

    const restoreMutation = useMutation({
        mutationFn: (id: string) => invoiceService.restoreInvoice(id),
        onSuccess: () => {
            message.success('Đã khôi phục hóa đơn thành công!');
            queryClient.invalidateQueries({ queryKey: ['trash-invoices'] });
            queryClient.invalidateQueries({ queryKey: ['invoices'] });
        },
        onError: (err: any) => {
            message.error('Lỗi khôi phục: ${err?.response?.data?.message || err.message}');
        },
    });

    const hardDeleteMutation = useMutation({
        mutationFn: (id: string) => invoiceService.hardDeleteInvoice(id),
        onSuccess: () => {
            message.success('Đã xóa vĩnh viễn hóa đơn thành công, dung lượng lưu trữ đã được giải phóng!');
            queryClient.invalidateQueries({ queryKey: ['trash-invoices'] });
        },
        onError: (err: any) => {
            message.error('Lỗi xóa vĩnh viễn: ${err?.response?.data?.message || err.message}');
        }
    });

    const handleRestore = (record: any) => {
        Modal.confirm({
            title: 'Khôi phục hóa đơn?',
            icon: <ExclamationCircleOutlined style={{ color: '#1890ff' }} />,
            content: <div>Hóa đơn <strong>{record.invoiceNumber}</strong> sẽ được đưa trở lại danh sách quản lý.</div>,
            okText: 'Khôi phục',
            cancelText: 'Hủy',
            onOk: () => restoreMutation.mutateAsync(record.invoiceId),
        });
    };

    const handleHardDelete = (record: any) => {
        Modal.confirm({
            title: 'Xóa vĩnh viễn hóa đơn?',
            icon: <ExclamationCircleOutlined style={{ color: '#ff4d4f' }} />,
            content: (
                <div>
                    <p>Bạn có chắc muốn xóa vĩnh viễn hóa đơn <strong>{record.invoiceNumber}</strong>?</p>
                    <p style={{ color: '#ff4d4f', fontWeight: 'bold' }}>Hành động này không thể hoàn tác, file gốc sẽ bị xóa vĩnh viễn và dung lượng hệ thống sẽ được hoàn trả lại cho công ty.</p>
                </div>
            ),
            okText: 'Xóa vĩnh viễn',
            okType: 'danger',
            cancelText: 'Hủy',
            onOk: () => hardDeleteMutation.mutateAsync(record.invoiceId),
        });
    };

    const columns = [
        {
            title: 'Số HĐ',
            dataIndex: 'invoiceNumber',
            key: 'invoiceNumber',
            render: (text: string, record: any) => (
                <Space direction="vertical" size={0}>
                    <Text strong>{text || 'N/A'}</Text>
                    <Text type="secondary" style={{ fontSize: 12 }}>{record.serialNumber}</Text>
                </Space>
            ),
        },
        {
            title: 'Ngày HĐ',
            dataIndex: 'invoiceDate',
            key: 'invoiceDate',
            render: (date: string) => {
                const dateObj = new Date(date);
                return isNaN(dateObj.getTime()) ? 'N/A' : dateObj.toLocaleDateString('vi-VN');
            },
        },
        {
            title: 'Người bán',
            dataIndex: 'sellerName',
            key: 'sellerName',
            ellipsis: true,
            render: (text: string, record: any) => (
                <Space direction="vertical" size={0}>
                    <Text ellipsis>{text || 'N/A'}</Text>
                    <Text type="secondary" style={{ fontSize: 12 }}>MST: {record.sellerTaxCode}</Text>
                </Space>
            ),
        },
        {
            title: 'Số tiền',
            dataIndex: 'totalAmount',
            key: 'totalAmount',
            align: 'right' as const,
            render: (val: number, record: any) => (
                <Text strong>{new Intl.NumberFormat('vi-VN').format(val || 0)} {record.invoiceCurrency}</Text>
            ),
        },
        {
            title: 'Rủi ro',
            dataIndex: 'riskLevel',
            key: 'riskLevel',
            render: (risk: string) => (
                <StatusBadge 
                  type="risk" 
                  value={risk === 'Green' ? 'Hợp lệ' : risk === 'Yellow' ? 'Cảnh báo' : risk === 'Red' ? 'Nguy hiểm' : 'Không xác định'} 
                />
            ),
        },
        {
            title: 'Trạng thái cũ',
            dataIndex: 'status',
            key: 'status',
            render: (status: string) => <StatusBadge value={status} type="status" />,
        },
        {
            title: 'Hành động',
            key: 'actions',
            align: 'center' as const,
            render: (_: any, record: any) => (
                <Space size="middle">
                    <Button 
                        type="text" 
                        icon={<RetweetOutlined style={{ color: '#1890ff' }} />} 
                        onClick={() => handleRestore(record)}
                        title="Khôi phục"
                    />
                    <Button 
                        type="text" 
                        danger
                        icon={<DeleteOutlined />} 
                        onClick={() => handleHardDelete(record)}
                        title="Xóa vĩnh viễn"
                    />
                </Space>
            ),
        },
    ];

    const invoices = invoiceData?.items || [];
    const totalInvoices = invoiceData?.totalCount || 0;

    return (
        <Space direction="vertical" style={{ width: '100%' }} size="large">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                    <Title level={4} style={{ margin: 0 }}>Thùng Rác</Title>
                    <Text type="secondary">Quản lý các hóa đơn đã xóa. Bạn có thể khôi phục hoặc xóa vĩnh viễn để giải phóng dung lượng.</Text>
                </div>
            </div>

            <Card style={{ borderRadius: 12, boxShadow: '0 2px 8px rgba(0,0,0,0.05)' }}>
                <Space style={{ marginBottom: 16 }} wrap>
                    <Input
                        placeholder="Tìm theo Số HĐ, MST..."
                        prefix={<SearchOutlined />}
                        value={searchText}
                        onChange={e => setSearchText(e.target.value)}
                        onPressEnter={() => {
                            setKeyword(searchText);
                            setPage(1);
                        }}
                        style={{ width: 250 }}
                    />
                    <Button onClick={() => { setKeyword(searchText); setPage(1); }}>Tìm kiếm</Button>
                </Space>

                <Table
                    columns={columns}
                    dataSource={invoices}
                    rowKey="invoiceId"
                    loading={isLoading}
                    pagination={{
                        current: page,
                        pageSize: pageSize,
                        total: totalInvoices,
                        showSizeChanger: true,
                        onChange: (p, s) => {
                            setPage(p);
                            setPageSize(s);
                        },
                    }}
                />
            </Card>
        </Space>
    );
};

export default TrashInvoiceList;
import React, { useState, useEffect } from 'react';
import {
    Card, Table, Tag, Input, Typography, Row, Col, Button, Space, Drawer, Switch, message, Avatar, Modal, Form, Select, Popconfirm
} from 'antd';
import {
    SearchOutlined, TeamOutlined, UserAddOutlined, KeyOutlined, CheckOutlined, CloseOutlined, DeleteOutlined
} from '@ant-design/icons';
import { userService, CompanyMemberDto, CreateCompanyMemberDto } from '@/services/user';

const { Title, Text } = Typography;
const { Option } = Select;

const AVAILABLE_PERMISSIONS = [
    { key: 'invoice:view', label: 'Xem hóa đơn', desc: 'Được phép xem danh sách và chi tiết hóa đơn trên hệ thống.' },
    { key: 'invoice:upload', label: 'Tải lên hóa đơn', desc: 'Được phép tải file XML/PDF hoặc tạo mới hóa đơn.' },
    { key: 'invoice:edit', label: 'Chỉnh sửa hóa đơn', desc: 'Được phép cập nhật, sửa đổi thông tin của hóa đơn.' },
    { key: 'invoice:approve', label: 'Duyệt hóa đơn', desc: 'Được phép phê duyệt hóa đơn hợp lệ (Cấp 1 hoặc Cấp 2).' },
    { key: 'invoice:reject', label: 'Từ chối hóa đơn', desc: 'Được phép từ chối và yêu cầu làm lại hóa đơn sai sót.' },
    { key: 'invoice:override_risk', label: 'Bỏ qua rủi ro', desc: 'Được phép ép duyệt các hóa đơn bị hệ thống cảnh báo rủi ro.' },
    { key: 'report:export', label: 'Xuất báo cáo', desc: 'Được phép trích xuất dữ liệu ra file Excel MISA/Tổng hợp.' }
];

const TeamManagement: React.FC = () => {
    const [data, setData] = useState<CompanyMemberDto[]>([]);
    const [loading, setLoading] = useState(false);

    // Manage Permissions Drawer
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const [selectedUser, setSelectedUser] = useState<CompanyMemberDto | null>(null);

    // Create User Modal
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [createForm] = Form.useForm();
    const [submitting, setSubmitting] = useState(false);

    useEffect(() => {
        fetchMembers();
    }, []);

    const fetchMembers = async () => {
        setLoading(true);
        try {
            const members = await userService.getCompanyMembers();
            setData(members);
        } catch (error: any) {
            message.error(error.response?.data?.message || 'Lỗi khi tải danh sách nhân sự');
        } finally {
            setLoading(false);
        }
    };

    const handleManagePerms = (record: CompanyMemberDto) => {
        setSelectedUser(record);
        setIsDrawerOpen(true);
    };

    const hasPermission = (user: CompanyMemberDto, permValue: string) => {
        if (!user.permissions) return false;
        return user.permissions.includes(permValue);
    };

    const handleTogglePerm = async (permKey: string, permValue: string, checked: boolean) => {
        if (!selectedUser) return;

        let currentPerms = selectedUser.permissions ? [...selectedUser.permissions] : [];
        if (checked && !currentPerms.includes(permValue)) {
            currentPerms.push(permValue);
        } else if (!checked) {
            currentPerms = currentPerms.filter(p => p !== permValue);
        }

        const updatedUser = { ...selectedUser, permissions: currentPerms };
        setSelectedUser(updatedUser);
        setData(prev => prev.map(u => u.id === updatedUser.id ? updatedUser : u));

        try {
            await userService.updateCompanyMember(updatedUser.id, {
                fullName: updatedUser.fullName,
                employeeId: updatedUser.employeeId || undefined,
                role: updatedUser.role,
                isActive: updatedUser.isActive,
                permissions: currentPerms
            });
            message.success(`Đã cập nhật quyền cho nhân viên ${updatedUser.fullName}`);
        } catch (error: any) {
            message.error(error.response?.data?.message || 'Cập nhật thất bại, đang hoàn tác...');
            // Rollback UI
            fetchMembers();
            if (isDrawerOpen) setIsDrawerOpen(false);
        }
    };

    const handleCreateUser = async (values: CreateCompanyMemberDto) => {
        setSubmitting(true);
        try {
            await userService.createCompanyMember(values);
            message.success('Đã gửi lời mời và tạo tài khoản thành công!');
            setIsCreateModalOpen(false);
            createForm.resetFields();
            fetchMembers();
        } catch (error: any) {
            message.error(error.response?.data?.message || 'Tạo nhân viên thất bại');
        } finally {
            setSubmitting(false);
        }
    };

    const handleDeleteUser = async (id: string) => {
        try {
            await userService.deleteCompanyMember(id);
            message.success('Đã xóa nhân viên khỏi hệ thống');
            fetchMembers();
        } catch (error: any) {
            message.error(error.response?.data?.message || 'Xóa nhân viên thất bại');
        }
    };

    const columns = [
        {
            title: 'Nhân viên',
            dataIndex: 'fullName',
            key: 'fullName',
            render: (text: string, record: CompanyMemberDto) => (
                <Space>
                    <Avatar style={{ backgroundColor: '#1a4b8c' }}>{text?.charAt(0)}</Avatar>
                    <div>
                        <Text strong>{text}</Text>
                        <br />
                        <Text type="secondary" style={{ fontSize: 12 }}>{record.email}</Text>
                    </div>
                </Space>
            ),
        },
        {
            title: 'Mã NV',
            dataIndex: 'employeeId',
            key: 'employeeId',
            render: (text: string) => <Text>{text || '-'}</Text>
        },
        {
            title: 'Phân quyền',
            dataIndex: 'role',
            key: 'role',
            render: (role: string) => (
                <Tag color={role === 'CompanyAdmin' ? 'geekblue' : 'default'} style={{ borderRadius: 12 }}>
                    {role === 'CompanyAdmin' ? 'Quản trị viên' : 'Nhân sự'}
                </Tag>
            ),
        },
        {
            title: 'Trạng thái',
            dataIndex: 'isActive',
            key: 'isActive',
            render: (isActive: boolean) => (
                <Tag color={isActive ? 'green' : 'red'}>{isActive ? 'Hoạt động' : 'Đã khóa'}</Tag>
            ),
        },
        {
            title: 'Hành động',
            key: 'action',
            render: (_, record: CompanyMemberDto) => (
                <Space>
                    <Button
                        type="default"
                        icon={<KeyOutlined />}
                        size="small"
                        onClick={() => handleManagePerms(record)}
                        disabled={record.role === 'CompanyAdmin'}
                    >
                        Phân quyền
                    </Button>
                    <Popconfirm
                        title="Xóa nhân viên này?"
                        description="Họ sẽ bị mất quyền truy cập vào công ty lập tức."
                        onConfirm={() => handleDeleteUser(record.id)}
                        okText="Xóa"
                        cancelText="Hủy"
                        okButtonProps={{ danger: true }}
                    >
                        <Button type="text" danger icon={<DeleteOutlined />} size="small" />
                    </Popconfirm>
                </Space>
            ),
        },
    ];

    return (
        <div className="animate-fade-in-up">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
                <div>
                    <Title level={4} style={{ margin: 0 }}>Quản lý đội ngũ & Phân quyền</Title>
                    <Text type="secondary">Cấp phát tài khoản và điều chỉnh quyền thao tác nghiệp vụ</Text>
                </div>
                <Button type="primary" icon={<UserAddOutlined />} onClick={() => setIsCreateModalOpen(true)}>
                    Mời nhân viên
                </Button>
            </div>

            <Card bordered={false} style={{ borderRadius: 12 }}>
                <Row style={{ marginBottom: 16 }}>
                    <Col span={8}>
                        <Input prefix={<SearchOutlined />} placeholder="Tìm kiếm theo Tên hoặc Email..." />
                    </Col>
                </Row>
                <Table columns={columns} dataSource={data} rowKey="id" loading={loading} />
            </Card>

            {/* Create User Modal */}
            <Modal
                title="Mời nhân sự mới"
                open={isCreateModalOpen}
                onCancel={() => setIsCreateModalOpen(false)}
                footer={null}
                destroyOnClose
            >
                <Form
                    form={createForm}
                    layout="vertical"
                    onFinish={handleCreateUser}
                    initialValues={{ role: 'Accountant' }}
                >
                    <Form.Item name="fullName" label="Họ và Tên" rules={[{ required: true, message: 'Vui lòng nhập họ tên' }]}>
                        <Input placeholder="Vd: Nguyễn Văn A" />
                    </Form.Item>
                    <Form.Item name="email" label="Email công việc" rules={[{ required: true, message: 'Vui lòng nhập email', type: 'email' }]}>
                        <Input placeholder="Vd: nva@congty.com" />
                    </Form.Item>
                    <Row gutter={16}>
                        <Col span={12}>
                            <Form.Item name="employeeId" label="Mã Nhân sự" rules={[{ required: true, message: 'Vui lòng nhập mã nhân viên' }]}>
                                <Input placeholder="Vd: NV001" />
                            </Form.Item>
                        </Col>
                        <Col span={12}>
                            <Form.Item 
                                name="role" 
                                label="Chức danh (Vai trò)" 
                                rules={[{ required: true, message: 'Vui lòng chọn chức danh' }]}
                            >
                                <Select 
                                    popupMatchSelectWidth={false} // Cho phép bảng xổ xuống to hơn cái ô input
                                    optionLabelProp="label"       // Khi chọn xong, chỉ hiển thị cái nhãn ngắn gọn
                                >
                                    <Option value="CompanyAdmin" label="Giám đốc">
                                        <div style={{ whiteSpace: 'normal', lineHeight: '1.4', padding: '4px 0', maxWidth: 300 }}>
                                            <Text strong>Giám đốc / Quản trị công ty</Text><br/>
                                            <Text type="secondary" style={{ fontSize: 12 }}>Toàn quyền hệ thống, nhân sự và duyệt hóa đơn.</Text>
                                        </div>
                                    </Option>
                                    <Option value="ChiefAccountant" label="Kế toán trưởng">
                                        <div style={{ whiteSpace: 'normal', lineHeight: '1.4', padding: '4px 0', maxWidth: 300 }}>
                                            <Text strong>Kế toán trưởng</Text><br/>
                                            <Text type="secondary" style={{ fontSize: 12 }}>Quản lý phòng kế toán, xét duyệt hóa đơn cấp cao.</Text>
                                        </div>
                                    </Option>
                                    <Option value="Accountant" label="Kế toán viên">
                                        <div style={{ whiteSpace: 'normal', lineHeight: '1.4', padding: '4px 0', maxWidth: 300 }}>
                                            <Text strong>Kế toán viên</Text><br/>
                                            <Text type="secondary" style={{ fontSize: 12 }}>Tải lên, xử lý dữ liệu và gửi yêu cầu duyệt.</Text>
                                        </div>
                                    </Option>
                                    <Option value="Viewer" label="Nhân viên (Chỉ xem)">
                                        <div style={{ whiteSpace: 'normal', lineHeight: '1.4', padding: '4px 0', maxWidth: 300 }}>
                                            <Text strong>Nhân viên (Chỉ xem)</Text><br/>
                                            <Text type="secondary" style={{ fontSize: 12 }}>Tra cứu và xuất báo cáo, không được chỉnh sửa.</Text>
                                        </div>
                                    </Option>
                                </Select>
                            </Form.Item>
                        </Col>
                    </Row>
                    <div style={{ marginTop: 16, display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                        <Button onClick={() => setIsCreateModalOpen(false)}>Hủy bỏ</Button>
                        <Button type="primary" htmlType="submit" loading={submitting}>Tạo tài khoản</Button>
                    </div>
                </Form>
            </Modal>

            <Drawer
                title={
                    <Space>
                        <KeyOutlined style={{ color: '#1a4b8c' }} />
                        <Text strong>Cấu hình quyền hạn: {selectedUser?.fullName}</Text>
                    </Space>
                }
                placement="right"
                onClose={() => setIsDrawerOpen(false)}
                open={isDrawerOpen}
                width={400}
            >
                {selectedUser && (
                    <Space direction="vertical" style={{ width: '100%' }} size={24}>
                        <div style={{ background: '#f5f5f5', padding: 16, borderRadius: 8 }}>
                            <Text strong style={{ fontSize: 13, color: '#1a4b8c' }}>THÔNG TIN NHÂN SỰ</Text>
                            <div style={{ marginTop: 8 }}>
                                <Text type="secondary">Email: </Text><Text>{selectedUser.email}</Text><br />
                                <Text type="secondary">Vai trò: </Text><Tag color="blue">{selectedUser.role}</Tag>
                            </div>
                        </div>

                        <div>
                            <Text strong style={{ fontSize: 13, color: '#1a4b8c' }}>CHI TIẾT QUYỀN HẠN (RBAC)</Text>
                            <div style={{ marginTop: 16, display: 'flex', flexDirection: 'column', gap: 16 }}>
                                {AVAILABLE_PERMISSIONS.map(perm => (
                                    <div key={perm.key} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <div style={{ paddingRight: 16 }}>
                                            <Text strong>{perm.label}</Text> <Text type="secondary" style={{ fontSize: 12 }}>({perm.key})</Text><br />
                                            <Text type="secondary" style={{ fontSize: 13 }}>{perm.desc}</Text>
                                        </div>
                                        <Switch
                                            checkedChildren={<CheckOutlined />} 
                                            unCheckedChildren={<CloseOutlined />}
                                            checked={hasPermission(selectedUser, perm.key)}
                                            onChange={(checked) => handleTogglePerm(perm.key, perm.key, checked)}
                                        />
                                    </div>
                                ))}
                            </div>
                        </div>
                    </Space>
                )}
            </Drawer>
        </div>
    );
};

export default TeamManagement;

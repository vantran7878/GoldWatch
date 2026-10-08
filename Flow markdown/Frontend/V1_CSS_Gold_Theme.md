# Frontend – CSS Styling: Gold Luxury Theme

> **Ngày thực hiện:** 08/10/2026
> **Branch:** `feature/frontend`
> **Người thực hiện:** Antigravity AI

---

## 📌 Mục tiêu

Làm đẹp giao diện Angular frontend cho **GoldWatch** theo phong cách:
- 🎨 **Theme:** Vàng kim, sáng, sang trọng
- 🔤 **Font:** Inter (Google Fonts) – đơn giản, dễ nhìn, chuyên nghiệp
- 📐 **Layout:** Centered card, responsive

---

## 📁 Các file đã chỉnh sửa

| File | Mô tả |
|------|-------|
| `src/styles.css` | Global styles, design tokens, reset |
| `src/app/app.css` | App shell – display block |
| `src/app/components/gold-price/gold-price.css` | Component styles chính |
| `src/app/components/gold-price/gold-price.html` | HTML cập nhật class mới |

---

## 🎨 Design System

### Bảng màu (CSS Variables)

```css
--gold-400: #fbbf24   /* Vàng sáng chính */
--gold-500: #f59e0b   /* Vàng accent */
--gold-600: #d97706   /* Vàng đậm – heading */
--gold-700: #b45309   /* Vàng tối – text nhấn */
--cream:    #fdf8f0   /* Background tổng */
```

### Font

```css
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');
font-family: 'Inter', system-ui, sans-serif;
```

---

## 🧩 Các thành phần UI đã tạo

### 1. Page Header
- Icon vàng (🏅) + tiêu đề **GoldWatch** với gradient text vàng
- Subtitle mô tả ngắn gọn

### 2. Card Container
- Bo tròn `border-radius: 20px`
- Shadow vàng nhẹ + border vàng `#fde68a`
- Nền trắng – nổi bật trên nền kem

### 3. Card Header
- Gradient nền vàng nhạt `#fffbeb -> #fef3c7`
- Badge "Trực tiếp" màu vàng với dot animation **pulse**

### 4. States (Trạng thái)

| State | Hiệu ứng |
|-------|---------|
| **Loading** | Spinner CSS animation (spin 0.8s) |
| **Error** | Icon + text màu amber đậm |
| **Empty** | Icon + text xám |

### 5. Data Table

| Element | Style |
|---------|-------|
| thead | Nền vàng nhạt gradient, chữ in hoa nhỏ |
| Row hover | background: #fffbeb (vàng rất nhạt) |
| Giá mua | color: #059669 (xanh lá) |
| Giá bán | color: #dc2626 (đỏ) |
| Nguồn | source-chip – badge vàng nhỏ |
| Thời gian | Xám nhạt, font nhỏ hơn |

### 6. Card Footer
- Hiển thị: số loại vàng + thời gian cập nhật cuối
- Nền vàng nhạt, text amber

---

## ✨ Animations

```css
/* Pulse dot trong badge "Trực tiếp" */
@keyframes pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50%       { opacity: 0.5; transform: scale(0.75); }
}

/* Spinner loading */
@keyframes spin {
  to { transform: rotate(360deg); }
}
```

---

## 📐 Responsive

- Wrapper `max-width: 960px`, căn giữa với padding
- Table có `overflow-x: auto` – scroll ngang trên mobile
- Padding body `48px 24px` – co lại trên màn nhỏ

---

## 🔤 Nguyên tắc thiết kế

1. **Không dùng Tailwind** – thuần Vanilla CSS với biến CSS
2. **Không placeholder** – tất cả màu sắc đều có chủ đích
3. **Tối giản nhưng đẳng cấp** – ít chi tiết thừa, nhiều khoảng trắng
4. **Micro-animation** – pulse dot, spinner, row hover
5. **Semantic color** – giá mua xanh / giá bán đỏ (quy ước thị trường VN)

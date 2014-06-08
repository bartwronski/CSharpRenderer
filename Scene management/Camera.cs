using System;
using System.Collections.Generic;
using System.Text;
using SlimDX;
using Math = System.Math;
using System.Windows.Forms;

namespace CSharpRenderer
{
    class Camera
    {
        public Matrix m_WorldToView;
        public Matrix m_ProjectionMatrix;
        public Matrix m_ViewProjectionMatrix;

        public Vector3 m_CameraPosition;
        public Vector3 m_CameraForward;
        public Vector3 m_CameraUp;
        public Vector3 m_CameraRight;
        public float m_Fov;
        public float m_NearZ;
        public float m_FarZ;
        public float m_AspectRatio;
        public bool m_Ortho;
        public float m_OrthoZoomX;
        public float m_OrthoZoomY;

        public float m_RotationX;
        public float m_RotationY;


        struct InputDesc
        {
            public bool wPressed;
            public bool sPressed;
            public bool aPressed;
            public bool dPressed;

            public float prevX;
            public float prevY;
        }
        InputDesc m_InputDesc;

        public Camera( bool ortho = false )
        {
            m_CameraPosition = new Vector3(0, 0, 0);
            m_CameraUp = new Vector3(0, 1, 0);
            m_CameraForward = new Vector3(0, 0, 1);
            m_CameraRight = Vector3.Cross(m_CameraUp, m_CameraForward);

            m_Fov = (float)(Math.PI / 2.0);
            m_NearZ = 0.1f;
            m_FarZ = 100.0f;
            m_AspectRatio = 1280.0f / 720.0f;
            this.m_Ortho = ortho;
            m_OrthoZoomX = 20.0f;
            m_OrthoZoomY = 20.0f;
            m_RotationX = 0.0f;
            m_RotationY = 0.0f;
        }

        public void CalculateMatrices()
        {
            m_CameraRight = Vector3.Cross(m_CameraUp, m_CameraForward);
            m_CameraRight.Normalize();
            // fov
            m_WorldToView = Matrix.LookAtLH(m_CameraPosition, m_CameraPosition+m_CameraForward, m_CameraUp );
            if ( !m_Ortho )
            {
                m_ProjectionMatrix = Matrix.PerspectiveFovLH(m_Fov, m_AspectRatio, m_NearZ, m_FarZ);
            }
            else
            {
                m_ProjectionMatrix = Matrix.OrthoLH(m_OrthoZoomX, m_OrthoZoomY, m_NearZ, m_FarZ);
            }
            
            m_ViewProjectionMatrix = m_WorldToView * m_ProjectionMatrix;
        }

        public void TickCamera( float delta )
        {
            if (m_InputDesc.wPressed)
            {
                m_CameraPosition += 0.2f * m_CameraForward;
            }
            if (m_InputDesc.sPressed)
            {
                m_CameraPosition -= 0.2f * m_CameraForward;
            }
            if (m_InputDesc.aPressed)
            {
                m_CameraPosition -= 0.2f * m_CameraRight;
            }
            if (m_InputDesc.dPressed)
            {
                m_CameraPosition += 0.2f * m_CameraRight;
            }

            Matrix camRotation = Matrix.RotationX(m_RotationY) * Matrix.RotationY(m_RotationX);
            Vector4 camForward = Vector3.Transform(new Vector3(0, 0, 1), camRotation);
            m_CameraForward.X = camForward.X;
            m_CameraForward.Y = camForward.Y;
            m_CameraForward.Z = camForward.Z;
        }

        public void BindToInput(Form form, Panel panel)
        {
            // handle alt+enter ourselves
            form.KeyDown += (o, e) =>
            {
                if (e.KeyCode == Keys.W)
                {
                    m_InputDesc.wPressed = true;
                }
                if (e.KeyCode == Keys.S)
                {
                    m_InputDesc.sPressed = true;
                }
                if (e.KeyCode == Keys.A)
                {
                    m_InputDesc.aPressed = true;
                }
                if (e.KeyCode == Keys.D)
                {
                    m_InputDesc.dPressed = true;
                }
            };

            form.KeyUp += (o, e) =>
            {
                if (e.KeyCode == Keys.W)
                {
                    m_InputDesc.wPressed = false;
                }
                if (e.KeyCode == Keys.S)
                {
                    m_InputDesc.sPressed = false;
                }
                if (e.KeyCode == Keys.A)
                {
                    m_InputDesc.aPressed = false;
                }
                if (e.KeyCode == Keys.D)
                {
                    m_InputDesc.dPressed = false;
                }
            };

            panel.MouseMove += (o, e) =>
            {
                float deltaX = (float)e.X - m_InputDesc.prevX;
                float deltaY = (float)e.Y - m_InputDesc.prevY;

                m_InputDesc.prevX = (float)e.X;
                m_InputDesc.prevY = (float)e.Y;

                if (e.Button == MouseButtons.Left)
                {
                    m_RotationX += deltaX / 90.0f;
                    m_RotationY += deltaY / 90.0f;
                    m_RotationY = Math.Min(Math.Max(m_RotationY, -(float)Math.PI / 2.0f + 0.01f), (float)Math.PI / 2.0f - 0.01f);
                }
               
            };
        }
    }
}

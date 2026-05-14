const errorHandler = (err, req, res, next) => {
  console.error('[ERROR]', err.message);
  console.error('[STACK]', err.stack);
  res.status(err.status || 500).json({
    success: false,
    data: null,
    error: {
      code: err.code || 'INTERNAL_ERROR',
      message: err.message || '서버 오류가 발생했습니다.',
    },
  });
};

module.exports = errorHandler;
